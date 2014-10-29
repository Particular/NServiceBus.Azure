namespace NServiceBus.DataBus
{
    using System;

    class AzureDataBusGuard
    {
        public static void CheckMaxRetries(object maxRetries)
        {
            if ((int)maxRetries < 0)
            {
                throw new Exception("MaxRetries should not be negative.");
            }
        }

        public static void CheckBackOffInterval(object backOffInterval)
        {
            if ((int)backOffInterval < 0)
            {
                throw new Exception("BackOffInterval should not be negative.");
            }
        }

        public static void CheckBlockSize(object blockSize)
        {
            if ((int)blockSize <= 0 || (int)blockSize > AzureDataBusDefaults.DefaultBlockSize)
            {
                throw new Exception("BlockSize should not be negative.");
            }            
        }

        public static void CheckNumberOfIOThreads(object numberOfIOThreads)
        {
            if ((int)numberOfIOThreads <= 0)
            {
                throw new Exception("NumberOfIOThreads should less than one.");
            }                        
        }

        public static void CheckConnectionString(object connectionString)
        {
            if (string.IsNullOrWhiteSpace((string)connectionString))
            {
                throw new Exception("ConnectionString should not be an empty string.");
            }
        }

        public static void CheckContainerName(object containerName)
        {
            if (string.IsNullOrWhiteSpace((string)containerName))
            {
                throw new Exception("Container name should not be an empty string.");
            }
        }

        public static void CheckBasePath(object basePath)
        {
            if ((string)basePath == null)
            {
                throw new Exception("BasePath name should not be an empty string.");
            }            
        }

        public static void CheckDefaultTTL(object defaultTTL)
        {
            if ((long)defaultTTL < 0)
            {
                throw new Exception("DefaultTTL should not be negative.");
            }            
        }
    }
}