namespace NServiceBus.Config
{
    using System.Configuration;
    using Timeout;

    /// <summary>
    /// 
    /// </summary>
    public class AzureTimeoutPersisterConfig : ConfigurationSection
    {
        public AzureTimeoutPersisterConfig()
        {
            Properties.Add(new ConfigurationProperty("ConnectionString", typeof(string), AzureTimeoutStorageDefaults.ConnectionString,
                null, new CallbackValidator(typeof(string), AzureTimeoutStorageGuard.CheckConnectionString), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("TimeoutManagerDataTableName", typeof(string), AzureTimeoutStorageDefaults.TimeoutManagerDataTableName,
                null, new CallbackValidator(typeof(string), AzureTimeoutStorageGuard.CheckTableName), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("TimeoutDataTableName", typeof(string), AzureTimeoutStorageDefaults.TimeoutDataTableName,
                null, new CallbackValidator(typeof(string), AzureTimeoutStorageGuard.CheckTableName), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("CatchUpInterval", typeof(int), AzureTimeoutStorageDefaults.CatchUpInterval,
                null, new CallbackValidator(typeof(int), AzureTimeoutStorageGuard.CheckCatchUpInterval), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("PartitionKeyScope", typeof(string), AzureTimeoutStorageDefaults.PartitionKeyScope,
                null, new CallbackValidator(typeof(string), AzureTimeoutStorageGuard.CheckPartitionKeyScope), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("CreateSchema", typeof(bool), AzureTimeoutStorageDefaults.CreateSchema, ConfigurationPropertyOptions.None));

        }

        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }

        public string TimeoutManagerDataTableName
        {
            get { return (string)this["TimeoutManagerDataTableName"]; }
            set { this["TimeoutManagerDataTableName"] = value; }
        }

        public string TimeoutDataTableName
        {
            get { return (string)this["TimeoutDataTableName"]; }
            set { this["TimeoutDataTableName"] = value; }
        }

        public int CatchUpInterval
        {
            get { return (int)this["CatchUpInterval"]; }
            set { this["CatchUpInterval"] = value; }
        }

        public string PartitionKeyScope
        {
            get { return (string)this["PartitionKeyScope"]; }
            set { this["PartitionKeyScope"] = value; }
        }

        public bool CreateSchema
        {
            get { return (bool)this["CreateSchema"]; }
            set { this["CreateSchema"] = value; }
        }
    }
}