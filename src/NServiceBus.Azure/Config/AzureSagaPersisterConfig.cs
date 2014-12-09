namespace NServiceBus.Config
{
    using System.Configuration;
    using NServiceBus.SagaPersisters;

    /// <summary>
    /// Config section for the Azure Saga Persister
    /// </summary>
    public class AzureSagaPersisterConfig:ConfigurationSection
    {
        public AzureSagaPersisterConfig()
        {
            base.Properties.Add(new ConfigurationProperty("ConnectionString", typeof(string), AzureStorageSagaDefaults.ConnectionString,
    null, new CallbackValidator(typeof(string), AzureStorageSagaGuard.CheckConnectionString), ConfigurationPropertyOptions.None));

            base.Properties.Add(new ConfigurationProperty("CreateSchema", typeof(bool), AzureStorageSagaDefaults.CreateSchema,
                ConfigurationPropertyOptions.None));

        }

        /// <summary>
        /// Connectionstring
        /// </summary>
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

        /// <summary>
        /// ´Determines if the database should be auto updated
        /// </summary>
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
    }
}