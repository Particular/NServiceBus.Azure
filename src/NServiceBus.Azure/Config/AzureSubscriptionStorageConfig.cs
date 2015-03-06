namespace NServiceBus.Config
{
    using System.Configuration;
    using Subscriptions;

    public class AzureSubscriptionStorageConfig : ConfigurationSection
    {
        public AzureSubscriptionStorageConfig()
        {
            Properties.Add(new ConfigurationProperty("ConnectionString", typeof(string), AzureSubscriptionStorageDefaults.ConnectionString,
                null, new CallbackValidator(typeof(string), AzureSubscriptionStorageGuard.CheckConnectionString), ConfigurationPropertyOptions.None));
            
            Properties.Add(new ConfigurationProperty("TableName", typeof(string), AzureSubscriptionStorageDefaults.TableName,
                null, new CallbackValidator(typeof(string), AzureSubscriptionStorageGuard.CheckTableName), ConfigurationPropertyOptions.None));

            Properties.Add(new ConfigurationProperty("CreateSchema", typeof(bool), AzureSubscriptionStorageDefaults.CreateSchema, 
                ConfigurationPropertyOptions.None));
        }

        public string ConnectionString
        {
            get
            {
                return this["ConnectionString"] as string;
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

        public bool CreateSchema
        {
            get
            {

                return (bool)this["CreateSchema"];
            }
            set
            {
                this["CreateSchema"] = value;
            }
        }

        public string TableName
        {
            get
            {
                return this["TableName"] as string;
            }
            set
            {
                this["TableName"] = value;
            }
        }
    }
}
