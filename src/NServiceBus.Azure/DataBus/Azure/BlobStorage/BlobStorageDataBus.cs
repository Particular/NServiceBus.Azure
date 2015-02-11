namespace NServiceBus.DataBus.Azure.BlobStorage
{
    using System.Globalization;
    using Logging;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;

    public class BlobStorageDataBus : IDataBus, IDisposable
    {
        static ILog logger = LogManager.GetLogger(typeof(IDataBus));
        CloudBlobContainer container;
        Timer timer;
        
        public int MaxRetries { get; set; }
        public int BackOffInterval { get; set; }
        public int NumberOfIOThreads { get; set; }
        public string BasePath { get; set; }
        public int BlockSize { get; set; }
        public long DefaultTTL { get; set; }

        public BlobStorageDataBus(CloudBlobContainer container)
        {
            this.container = container;
            timer = new Timer(o => DeleteExpiredBlobs());
        }

        public Stream Get(string key)
        {
            var stream = new MemoryStream();
            var blob = container.GetBlockBlobReference(Path.Combine(BasePath, key));
            DownloadBlobInParallel(blob, stream);
            return stream;
        }

        public string Put(Stream stream, TimeSpan timeToBeReceived)
        {
            var key = Guid.NewGuid().ToString();
            var blob = container.GetBlockBlobReference(Path.Combine(BasePath, key));
            SetValidUntil(blob, timeToBeReceived);
            UploadBlobInParallel(blob, stream);
            return key;
        }
        public void Start()
        {
            ServicePointManager.DefaultConnectionLimit = NumberOfIOThreads;
            container.CreateIfNotExists();
            timer.Change(0, 300000);
            logger.Info("Blob storage data bus started. Location: " + BasePath);
        }

        public void Dispose()
        {
            timer.Dispose();

            DeleteExpiredBlobs();

            logger.Info("Blob storage data bus stopped");
        }

        void DeleteExpiredBlobs()
        {
            try
            {
                var blobs = container.ListBlobs();
                foreach (var blockBlob in blobs.Select(blob => blob as CloudBlockBlob))
                {
                    if (blockBlob == null) continue;

                    blockBlob.FetchAttributes();
                    var validUntil = GetValidUntil(blockBlob, DefaultTTL);
                    if (validUntil < DateTime.UtcNow)
                    {
                        blockBlob.DeleteIfExists();
                    }
                }
            }
            catch (StorageException ex) // needs to stay as it runs on a background thread
            {
                logger.Warn(ex.Message);
            }
        }


        internal static void SetValidUntil(ICloudBlob blob, TimeSpan timeToBeReceived)
        {
            if (timeToBeReceived != TimeSpan.MaxValue)
            {
                var validUntil = DateTime.UtcNow + timeToBeReceived;
                blob.Metadata["ValidUntilUtc"] = DateTimeExtensions.ToWireFormattedString(validUntil);
            }
            // else no ValidUntil will be considered it to be non-expiring or subject to maximum ttl
        }


        internal static DateTime GetValidUntil(ICloudBlob blockBlob, long defaultTtl = Int64.MaxValue)
        {
            string validUntilUtcString;
            if (blockBlob.Metadata.TryGetValue("ValidUntilUtc", out validUntilUtcString))
            {
                return DateTimeExtensions.ToUtcDateTime(validUntilUtcString);
                    
            }

            string validUntilString;
            if (!blockBlob.Metadata.TryGetValue("ValidUntil", out validUntilString))
            {
                // no ValidUntil and no ValidUntilUtc will be considered non-expiring or whatever default ttl is set
                return ToDefault(defaultTtl, blockBlob);
            }
            var style = DateTimeStyles.AssumeUniversal;
            if (!blockBlob.Metadata.ContainsKey("ValidUntilKind"))
            {
                style = DateTimeStyles.AdjustToUniversal;
            }

            DateTime validUntil;
            //since this is the old version that could be written in any culture we cannot be certain it will parse so need to handle failure
            if (!DateTime.TryParse(validUntilString, null, style, out validUntil))
            {
                var message = string.Format("Could not parse the 'ValidUntil' value `{0}` for blob {1}. Resetting 'ValidUntil' to not expire. You may consider manually removing this entry if non-expiry is incorrect.", validUntilString, blockBlob.Uri);
                logger.Error(message);
                //If we cant parse the datetime then assume data corruption and store for max time
                SetValidUntil(blockBlob, TimeSpan.MaxValue);
                //upload the changed metadata
                blockBlob.SetMetadata();

                return ToDefault(defaultTtl, blockBlob);
            }

            return validUntil.ToUniversalTime();
        }

        static DateTime ToDefault(long defaultTtl, ICloudBlob blockBlob)
        {
            if (defaultTtl != Int64.MaxValue && blockBlob.Properties.LastModified.HasValue)
            {
                try
                {
                    return blockBlob.Properties.LastModified.Value.Add(TimeSpan.FromSeconds(defaultTtl)).UtcDateTime;
                }
                catch (ArgumentOutOfRangeException) 
                {
                    // fallback to datetime.maxvalue
                }
            }

            return DateTime.MaxValue;
        }

        void UploadBlobInParallel(CloudBlockBlob blob, Stream stream)
        {
            blob.ServiceClient.ParallelOperationThreadCount = NumberOfIOThreads;
            container.ServiceClient.RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(BackOffInterval), MaxRetries);
            blob.StreamWriteSizeInBytes = BlockSize;
            blob.UploadFromStream(stream);
        }

        void DownloadBlobInParallel(CloudBlockBlob blob, Stream stream)
        {
            blob.FetchAttributes();
            blob.ServiceClient.ParallelOperationThreadCount = NumberOfIOThreads;
            container.ServiceClient.RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(BackOffInterval), MaxRetries);
            blob.DownloadToStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
        }

    }
}
