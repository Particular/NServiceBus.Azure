using System.Configuration;

namespace NServiceBus.Config
{
    public class AzureDataBusConfig : ConfigurationSection
    {
        [ConfigurationProperty("MaxRetries", IsRequired = false, DefaultValue = AzureDataBusDefaults.DefaultMaxRetries)]
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

        [ConfigurationProperty("BlockSize", IsRequired = false, DefaultValue = AzureDataBusDefaults.DefaultBlockSize)]
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

        [ConfigurationProperty("NumberOfIOThreads", IsRequired = false, DefaultValue = AzureDataBusDefaults.DefaultNumberOfIOThreads)]
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

        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = AzureDataBusDefaults.DefaultConnectionString)]
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

        [ConfigurationProperty("Container", IsRequired = false, DefaultValue = AzureDataBusDefaults.Defaultcontainer)]
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

        [ConfigurationProperty("BasePath", IsRequired = false, DefaultValue = AzureDataBusDefaults.DefaultBasePath)]
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
    }
}