namespace NServiceBus
{
    public class AzureDataBusDefaults
    {
        public const string Defaultcontainer = "databus";
        public const string DefaultBasePath = "";
        public const int DefaultMaxRetries = 5;
        public const int DefaultNumberOfIOThreads = 5;
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";
        public const int DefaultBlockSize = 4 * 1024 * 1024; // 4MB
        public const int DefaultBackOffInterval = 30; //seconds
    }
}