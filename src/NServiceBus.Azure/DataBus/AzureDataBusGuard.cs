namespace NServiceBus.DataBus
{
    using System;

    class AzureDataBusGuard
    {
        public static void CheckMaxRetries(object maxRetries)
        {
            if ((int)maxRetries < 0)
            {
                throw new ArgumentOutOfRangeException("maxRetries", maxRetries, "MaxRetries should not be negative.");
            }
        }

        public static void CheckBackOffInterval(object backOffInterval)
        {
            if ((int)backOffInterval < 0)
            {
                throw new ArgumentOutOfRangeException("backOffInterval", backOffInterval, "BackOffInterval should not be negative.");
            }
        }

        public static void CheckBlockSize(object blockSize)
        {
            if ((int)blockSize <= 0 || (int)blockSize > AzureDataBusDefaults.DefaultBlockSize)
            {
                throw new ArgumentOutOfRangeException("blockSize", blockSize, "BlockSize should not be negative.");
            }            
        }

        public static void CheckNumberOfIOThreads(object numberOfIOThreads)
        {
            if ((int)numberOfIOThreads <= 0)
            {
                throw new ArgumentOutOfRangeException("numberOfIOThreads", numberOfIOThreads, "NumberOfIOThreads should not be less than one.");
            }                        
        }

        public static void CheckConnectionString(object connectionString)
        {
            if (string.IsNullOrWhiteSpace((string)connectionString))
            {
                throw new ArgumentException("ConnectionString should not be an empty string.", "connectionString");
            }
        }

        public static void CheckContainerName(object containerName)
        {
            if (string.IsNullOrWhiteSpace((string)containerName))
            {
                throw new ArgumentException("Container name should not be an empty string.", "containerName");
            }
        }

        public static void CheckBasePath(object basePath)
        {
            var value = basePath != null ? (string)basePath : " ";
            var spacesOnly = value.Trim().Length == 0 && value.Length != 0;

            if (spacesOnly)
            {
                throw new ArgumentException("BasePath name should not be null or spaces only.", "basePath");
            }            
        }

        public static void CheckDefaultTTL(object defaultTTL)
        {
            if (defaultTTL.GetType() != typeof(long))
            {
                throw new ArgumentException("defaultTTL should be of type long", "defaultTTL");
            }
            if ((long)defaultTTL < 0)
            {
                throw new ArgumentOutOfRangeException("defaultTTL", defaultTTL, "DefaultTTL should not be negative.");
            }            
        }

        public static void CheckCleanupInterval(object cleanupInterval)
        {
            if ((int)cleanupInterval < 0)
            {
                throw new ArgumentOutOfRangeException("cleanupInterval", cleanupInterval, "CleanupInterval should not be negative.");
            }
        }
    }
}