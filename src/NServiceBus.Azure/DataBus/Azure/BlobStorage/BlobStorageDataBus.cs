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

    public class BlobStorageDataBus : IDataBus, IDisposable
    {
        ILog logger = LogManager.GetLogger(typeof(IDataBus));
        CloudBlobContainer container;
        Timer timer;
        
        public int MaxRetries { get; set; }
        public int NumberOfIOThreads { get; set; }
        public string BasePath { get; set; }
        public int BlockSize { get; set; }

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
	        blob.Metadata["ValidUntil"] = (timeToBeReceived == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow + timeToBeReceived).ToString();
            blob.Metadata["ValidUntilKind"] = "Utc";
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
                    DateTime validUntil;
                    var style = DateTimeStyles.AssumeUniversal;

                    if (!blockBlob.Metadata.ContainsKey("ValidUntilKind"))
                    {
                        style = DateTimeStyles.AdjustToUniversal;
                    }
                    
                    DateTime.TryParse(blockBlob.Metadata["ValidUntil"], null, style, out validUntil);

                    if (validUntil == default(DateTime) || validUntil < DateTime.UtcNow)
                        blockBlob.DeleteIfExists();
                }
            }
            catch (StorageException ex) // needs to stay as it runs on a background thread
            {
                logger.Warn(ex.Message);
            }
        }

        void UploadBlobInParallel(CloudBlockBlob blob, Stream stream)
        {
            blob.ServiceClient.ParallelOperationThreadCount = NumberOfIOThreads;
            blob.UploadFromStream(stream);
        }

        void DownloadBlobInParallel(CloudBlockBlob blob, Stream stream)
        {
            blob.FetchAttributes();
            blob.ServiceClient.ParallelOperationThreadCount = NumberOfIOThreads;
            blob.DownloadToStream(stream);
            stream.Seek(0, SeekOrigin.Begin);
        }

    }
}
