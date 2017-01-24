namespace NServiceBus
{
    using System;

    public class AzureDataBusDefaults
    {
        public const string DefaultContainer = "databus";
        public const string DefaultBasePath = "";
        public const int DefaultMaxRetries = 5;
        public const int DefaultNumberOfIOThreads = 5;
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";
        public const int DefaultBlockSize = 4 * 1024 * 1024; // Maximum 4MB
        public const int DefaultBackOffInterval = 30; // seconds
        public const long DefaultTTL = Int64.MaxValue; // seconds
        public const int DefaultCleanupInterval = 300000; // milli seconds
    }
}