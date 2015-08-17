namespace NServiceBus.Config
{
    using System.Configuration;
    using DataBus;

    public class AzureDataBusConfig : ConfigurationSection
    {
        public AzureDataBusConfig()
        {
            Properties.Add(new ConfigurationProperty("MaxRetries", typeof(int), AzureDataBusDefaults.DefaultMaxRetries,
                null, new CallbackValidator(typeof(int), AzureDataBusGuard.CheckMaxRetries), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("BackOffInterval", typeof(int), AzureDataBusDefaults.DefaultBackOffInterval,
                null, new CallbackValidator(typeof(int), AzureDataBusGuard.CheckBackOffInterval), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("BlockSize", typeof(int), AzureDataBusDefaults.DefaultBlockSize,
                null, new CallbackValidator(typeof(int), AzureDataBusGuard.CheckBlockSize), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("NumberOfIOThreads", typeof(int), AzureDataBusDefaults.DefaultNumberOfIOThreads,
                null, new CallbackValidator(typeof(int), AzureDataBusGuard.CheckNumberOfIOThreads), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("ConnectionString", typeof(string), AzureDataBusDefaults.DefaultConnectionString,
                null, new CallbackValidator(typeof(string), AzureDataBusGuard.CheckConnectionString), ConfigurationPropertyOptions.None));
            
            Properties.Add(new ConfigurationProperty("Container", typeof(string), AzureDataBusDefaults.DefaultContainer,
                null, new CallbackValidator(typeof(string), AzureDataBusGuard.CheckContainerName), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("BasePath", typeof(string), AzureDataBusDefaults.DefaultBasePath,
                null, new CallbackValidator(typeof(string), AzureDataBusGuard.CheckBasePath), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("DefaultTTL", typeof(long), AzureDataBusDefaults.DefaultTTL,
                null, new CallbackValidator(typeof(long), AzureDataBusGuard.CheckDefaultTTL), ConfigurationPropertyOptions.None));
        }

        public int MaxRetries
        {
            get
            {
                return (int)this["MaxRetries"];
            }
            set
            {
                this["MaxRetries"] = value;
            }
        }

        public int BackOffInterval
        {
            get
            {
                return (int)this["BackOffInterval"];
            }
            set
            {
                this["BackOffInterval"] = value;
            }
        }

        public int BlockSize
        {
            get
            {
                return (int)this["BlockSize"];
            }
            set
            {
                this["BlockSize"] = value;
            }
        }

        public int NumberOfIOThreads
        {
            get
            {
                return (int)this["NumberOfIOThreads"];
            }
            set
            {
                this["NumberOfIOThreads"] = value;
            }
        }

        public string ConnectionString
        {
            get
            {
                return (string)this["ConnectionString"];
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

        public string Container
        {
            get
            {
                return (string)this["Container"];
            }
            set
            {
                this["Container"] = value;
            }
        }

        public string BasePath
        {
            get
            {
                return (string)this["BasePath"];
            }
            set
            {
                this["BasePath"] = value;
            }
        }

        public long DefaultTTL
        {
            get
            {
                return (long)this["DefaultTTL"];
            }
            set
            {
                this["DefaultTTL"] = value;
            }
        }
    }
}