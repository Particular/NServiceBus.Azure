namespace NServiceBus.Unicast.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Azure;
    using MessageDrivenSubscriptions;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Table.DataServices;

    /// <summary>
    /// 
    /// </summary>
    public class AzureSubscriptionStorage : ISubscriptionStorage
    {
        CloudTableClient client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        public AzureSubscriptionStorage(CloudStorageAccount account)
        {
            client = account.CreateCloudTableClient();           
        }

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            using (var context = new SubscriptionServiceContext(client))
            {
                foreach (var messageType in messageTypes)
                {
                    try
                    {
                        var subscription = new Subscription
                        {
                            RowKey = EncodeTo64(address.ToString()),
                            PartitionKey = messageType.ToString()
                        };

                        context.AddObject(SubscriptionServiceContext.SubscriptionTableName, subscription);
                        context.SaveChangesWithRetries();
                    }
                    catch (StorageException ex)
                    {
                        if (ex.RequestInformation.HttpStatusCode != 409) throw;
                    }
                   
                }
            }
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            using (var context = new SubscriptionServiceContext(client))
            {
                var encodedAddress = EncodeTo64(address.ToString());
                foreach (var messageType in messageTypes)
                {
                    var type = messageType;
                    var query = from s in context.Subscriptions
                                where s.PartitionKey == type.ToString() && s.RowKey == encodedAddress
                                select s;

                    var subscription = query
                        .AsTableServiceQuery(context) // Fixes #191
                        .AsEnumerable() // Fixes #191, continuation not applied on single resultsets eventhough continuation can happen
                        .SafeFirstOrDefault();
                    if(subscription != null) context.DeleteObject(subscription);
                    context.SaveChangesWithRetries();
                }
            }
        }



        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var subscribers = new HashSet<Address>();

            using (var context = new SubscriptionServiceContext(client))
            {
                foreach (var messageType in messageTypes)
                {
                    var name = messageType.TypeName;
                    var lowerBound = name;
                    var upperBound = GetUpperBound(name);

                    var query = from s in context.Subscriptions
                        where s.PartitionKey.CompareTo(lowerBound) >= 0 &&
                              s.PartitionKey.CompareTo(upperBound) < 0
                        select s;

                    var result = query
                        .AsTableServiceQuery(context) // Fixes #191
                        .ToList();

                    foreach (var subscriber in result.Select(s => Address.Parse(DecodeFrom64(s.RowKey))))
                    {
                        subscribers.Add(subscriber);
                    }
                }
            }
          
            return subscribers;
        }

        static string GetUpperBound(string name)
        {
            return name + ", Version=z";
        }

        public void Init()
        {
            //No-op
        }

        static string EncodeTo64(string toEncode)
        {
            var toEncodeAsBytes = System.Text.Encoding.ASCII.GetBytes(toEncode);
            var returnValue = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static string DecodeFrom64(string encodedData)
        {
            var encodedDataAsBytes = Convert.FromBase64String(encodedData);
            var returnValue = System.Text.Encoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
    }
}
