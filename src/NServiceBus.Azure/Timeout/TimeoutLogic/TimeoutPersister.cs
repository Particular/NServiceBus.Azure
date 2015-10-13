﻿namespace NServiceBus.Azure
{
    using System.Text.RegularExpressions;
    using Config;
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.IO;
    using System.Linq;
    using System.Web.Script.Serialization;
    using Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Table.DataServices;
    using Support;
    using Timeout.Core;
    
    public class TimeoutPersister : IPersistTimeouts, IDetermineWhoCanSend, IPersistTimeoutsV2
    {
        Configure config;

        public TimeoutPersister(Configure config)
        {
            this.config = config;
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var results = new List<Tuple<string, DateTime>>();
           
            var now = DateTime.UtcNow;
            var context = new ServiceContext(account.CreateCloudTableClient()) {IgnoreResourceNotFoundException = true};
            TimeoutManagerDataEntity lastSuccessfulReadEntity;
            var lastSuccessfulRead = TryGetLastSuccessfulRead(context, out lastSuccessfulReadEntity)
                                            ? lastSuccessfulReadEntity.LastSuccessfullRead
                                            : default(DateTime?);

            IOrderedEnumerable<TimeoutDataEntity> result;
            IQueryable<TimeoutDataEntity> query;

            if (lastSuccessfulRead.HasValue)
            {
                query = from c in context.TimeoutData
                            where c.PartitionKey.CompareTo(lastSuccessfulRead.Value.ToString(PartitionKeyScope)) >= 0
                            && c.PartitionKey.CompareTo(now.ToString(PartitionKeyScope)) <= 0
                                && c.OwningTimeoutManager == config.Settings.EndpointName()
                            select c;
            }
            else
            {
                query = from c in context.TimeoutData
                          where c.OwningTimeoutManager == config.Settings.EndpointName()
                            select c;
            }

            result = query
                        .AsTableServiceQuery(context) // fixes issue #191
                        .ToList().OrderBy(c => c.Time);

            var allTimeouts = result.ToList();
            if (allTimeouts.Count == 0)
            {
                nextTimeToRunQuery = now.AddSeconds(1);
                return results;
            }

            var pastTimeouts = allTimeouts.Where(c => c.Time > startSlice && c.Time <= now).ToList();
            var futureTimeouts = allTimeouts.Where(c => c.Time > now).ToList();

            if (lastSuccessfulReadEntity != null && lastSuccessfulRead.HasValue)
            {
                var catchingUp = lastSuccessfulRead.Value.AddSeconds(CatchUpInterval);
                lastSuccessfulRead = catchingUp > now ? now : catchingUp;
                lastSuccessfulReadEntity.LastSuccessfullRead = lastSuccessfulRead.Value;
            }

            var future = futureTimeouts.SafeFirstOrDefault();
            nextTimeToRunQuery = lastSuccessfulRead.HasValue ? lastSuccessfulRead.Value
                                        : (future != null ? future.Time : now.AddSeconds(1));
                
            results = pastTimeouts
                .Where(c => !string.IsNullOrEmpty(c.RowKey))
                .Select(c => new Tuple<String, DateTime>(c.RowKey, c.Time))
                .Distinct()
                .ToList();

            UpdateSuccessfulRead(context, lastSuccessfulReadEntity);
           
            return results;
        }

        public void Add(TimeoutData timeout)
        {
            var context = new ServiceContext(account.CreateCloudTableClient()){ IgnoreResourceNotFoundException = true};

            string identifier;
            timeout.Headers.TryGetValue(Headers.MessageId, out identifier);
            if (string.IsNullOrEmpty(identifier)) { identifier = Guid.NewGuid().ToString(); }

            TimeoutDataEntity timeoutDataEntity;
            if (TryGetTimeoutData(context, identifier, string.Empty, out timeoutDataEntity)) return;

            var stateAddress = Upload(timeout.State, identifier);
            var headers = Serialize(timeout.Headers);

            if (!TryGetTimeoutData(context, timeout.Time.ToString(PartitionKeyScope), stateAddress, out timeoutDataEntity))
                context.AddObject(ServiceContext.TimeoutDataTableName,
                                      new TimeoutDataEntity(timeout.Time.ToString(PartitionKeyScope), stateAddress)
                                      {
                                          Destination = timeout.Destination.ToString(),
                                          SagaId = timeout.SagaId,
                                          StateAddress = stateAddress,
                                          Time = timeout.Time,
                                          OwningTimeoutManager = timeout.OwningTimeoutManager,
                                          Headers = headers
                                      });

            timeout.Id = stateAddress;

            if (timeout.SagaId != default(Guid) && !TryGetTimeoutData(context, timeout.SagaId.ToString(), stateAddress, out timeoutDataEntity))
                context.AddObject(ServiceContext.TimeoutDataTableName,
                                      new TimeoutDataEntity(timeout.SagaId.ToString(), stateAddress)
                                      {
                                          Destination = timeout.Destination.ToString(),
                                          SagaId = timeout.SagaId,
                                          StateAddress = stateAddress,
                                          Time = timeout.Time,
                                          OwningTimeoutManager = timeout.OwningTimeoutManager,
                                          Headers = headers
                                      });

            context.AddObject(ServiceContext.TimeoutDataTableName,
                                new TimeoutDataEntity(stateAddress, string.Empty)
                                {
                                    Destination = timeout.Destination.ToString(),
                                    SagaId = timeout.SagaId,
                                    StateAddress = stateAddress,
                                    Time = timeout.Time,
                                    OwningTimeoutManager = timeout.OwningTimeoutManager,
                                    Headers = headers
                                });

            context.SaveChanges();
        }

        public TimeoutData Peek(string timeoutId)
        {
            var context = new ServiceContext(account.CreateCloudTableClient()) { IgnoreResourceNotFoundException = true };

            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(context, timeoutId, string.Empty, out timeoutDataEntity))
            {
                return null;
            }

            var timeoutData = new TimeoutData
            {
                Destination = Address.Parse(timeoutDataEntity.Destination),
                SagaId = timeoutDataEntity.SagaId,
                State = Download(timeoutDataEntity.StateAddress),
                Time = timeoutDataEntity.Time,
                Id = timeoutDataEntity.RowKey,
                OwningTimeoutManager = timeoutDataEntity.OwningTimeoutManager,
                Headers = Deserialize(timeoutDataEntity.Headers)
            };
            return timeoutData;
        }

        public bool TryRemove(string timeoutId)
        {
            try
            {
                TimeoutData data;
                return TryRemove(timeoutId, out data);
            }
            catch (DataServiceRequestException) // table entries were already removed
            {
                return false;
            }
            catch (StorageException) // blob file was already removed
            {
                return false;
            }
        }


        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            timeoutData = null;

            var context = new ServiceContext(account.CreateCloudTableClient()) { IgnoreResourceNotFoundException = true};
            
            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(context, timeoutId, string.Empty, out timeoutDataEntity))
            {
                return false;
            }

            timeoutData = new TimeoutData
            {
                Destination = Address.Parse(timeoutDataEntity.Destination),
                SagaId = timeoutDataEntity.SagaId,
                State = Download(timeoutDataEntity.StateAddress),
                Time = timeoutDataEntity.Time,
                Id = timeoutDataEntity.RowKey,
                OwningTimeoutManager = timeoutDataEntity.OwningTimeoutManager,
                Headers = Deserialize(timeoutDataEntity.Headers)
            };

            TimeoutDataEntity timeoutDataEntityBySaga;
            if (TryGetTimeoutData(context, timeoutDataEntity.SagaId.ToString(), timeoutId, out timeoutDataEntityBySaga))
            {
                context.DeleteObject(timeoutDataEntityBySaga);
            }

            TimeoutDataEntity timeoutDataEntityByTime;
            if (TryGetTimeoutData(context, timeoutDataEntity.Time.ToString(PartitionKeyScope), timeoutId, out timeoutDataEntityByTime))
            {
                context.DeleteObject(timeoutDataEntityByTime);
            }

            RemoveState(timeoutDataEntity.StateAddress);

            context.DeleteObject(timeoutDataEntity);

            context.SaveChanges();

            return true;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            var context = new ServiceContext(account.CreateCloudTableClient());
            
            var query = (from c in context.TimeoutData
                where c.PartitionKey == sagaId.ToString()
                select c);

            var results = query
                .AsTableServiceQuery(context) // fixes issue #191
               .ToList();

            foreach (var timeoutDataEntityBySaga in results)
            {
                RemoveState(timeoutDataEntityBySaga.StateAddress);

                TimeoutDataEntity timeoutDataEntityByTime;
                if (TryGetTimeoutData(context, timeoutDataEntityBySaga.Time.ToString(PartitionKeyScope), timeoutDataEntityBySaga.RowKey, out timeoutDataEntityByTime))
                    context.DeleteObject(timeoutDataEntityByTime);

                TimeoutDataEntity timeoutDataEntity;
                if (TryGetTimeoutData(context, timeoutDataEntityBySaga.RowKey, string.Empty, out timeoutDataEntity))
                    context.DeleteObject(timeoutDataEntity);

                context.DeleteObject(timeoutDataEntityBySaga);
            }
            context.SaveChanges();

        }

        bool TryGetTimeoutData(ServiceContext context, string partitionKey, string rowKey, out TimeoutDataEntity result)
        {
            result = (from c in context.TimeoutData
                      where c.PartitionKey == partitionKey && c.RowKey == rowKey // issue #191 cannot occur when both partitionkey and rowkey are specified
                      select c).SafeFirstOrDefault();

            return result != null;

        }

        public bool CanSend(TimeoutData data)
        {
            var context = new ServiceContext(account.CreateCloudTableClient());
            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(context, data.Id, string.Empty, out timeoutDataEntity)) return false;

            var leaseBlob = container.GetBlockBlobReference(timeoutDataEntity.StateAddress);

            using (var lease = new AutoRenewLease(leaseBlob))
            {
                return lease.HasLease;
            }
        }

        public string ConnectionString
        {
            get
            {
                return connectionString;
            }
            set
            {
                connectionString = value;
                Init(connectionString);
            }
        }

        public int CatchUpInterval { get; set; }
        public string PartitionKeyScope { get; set; }

        void Init(string connectionString)
        {
            account = CloudStorageAccount.Parse(connectionString);
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference(ServiceContext.TimeoutManagerDataTableName);
            table.CreateIfNotExists();
            table = tableClient.GetTableReference(ServiceContext.TimeoutDataTableName);
            table.CreateIfNotExists();
            container = account.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            container.CreateIfNotExists();
        }

        string Upload(byte[] state, string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream(state))
            {
                blob.UploadFromStream(stream);
            }
            return stateAddress;
        }

        byte[] Download(string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream())
            {
                blob.DownloadToStream(stream);
                stream.Position = 0;

                var buffer = new byte[16*1024];
                using (var ms = new MemoryStream())
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
        }

        string Serialize(Dictionary<string, string> headers)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(headers);
        }

        Dictionary<string, string> Deserialize(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                return new Dictionary<string, string>();
            }

            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<Dictionary<string, string>>(state);
        }

        void RemoveState(string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            blob.DeleteIfExists();
        }

        string GetUniqueEndpointName()
        {
            var identifier = SafeRoleEnvironment.IsAvailable ? SafeRoleEnvironment.CurrentRoleInstanceId : RuntimeEnvironment.MachineName;

            return Sanitize(config.Settings.EndpointName() + "_" + identifier);
        }

        string Sanitize(string s)
        {
            var rgx = new Regex(@"[^a-zA-Z0-9\-_]");
            var n = rgx.Replace(s, "");
            return n;
        }

        bool TryGetLastSuccessfulRead(ServiceContext context, out TimeoutManagerDataEntity lastSuccessfulReadEntity)
        {
            var query = from m in context.TimeoutManagerData
                                            where m.PartitionKey == GetUniqueEndpointName()
                                        select m;

            lastSuccessfulReadEntity = query
                .AsTableServiceQuery(context)
                .AsEnumerable() //TSQ does only follows continuation tokens for listings, not for single entity results, yet continuation tokes can still happen in this case
                .SafeFirstOrDefault();
            
            return lastSuccessfulReadEntity != null;
        }

        void UpdateSuccessfulRead(ServiceContext context, TimeoutManagerDataEntity read)
        {
            try
            {
                if (read == null)
                {
                    read = new TimeoutManagerDataEntity(GetUniqueEndpointName(), string.Empty)
                           {
                               LastSuccessfullRead = DateTime.UtcNow
                           };

                    context.AddObject(ServiceContext.TimeoutManagerDataTableName, read);
                }
                else
                {
                    context.Detach(read);
                    context.AttachTo(ServiceContext.TimeoutManagerDataTableName, read, "*");
                    context.UpdateObject(read);
                }
                context.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
            }
            catch (DataServiceRequestException ex) // handle concurrency issues
            {
                var response = ex.Response.FirstOrDefault();
                //Concurrency Exception - PreCondition Failed or Entity Already Exists
                if (response != null && (response.StatusCode == 412 || response.StatusCode == 409))
                {
                    return; 
                    // I assume we can ignore this condition? 
                    // Time between read and update is very small, meaning that another instance has sent 
                    // the timeout messages that this node intended to send and if not we will resend 
                    // anything after the other node's last read value anyway on next request.
                }

                throw;
            }

        }

        string connectionString;
        CloudStorageAccount account;
        CloudBlobContainer container;

        static ILog Logger = LogManager.GetLogger(typeof(TimeoutPersister));
    }
}
