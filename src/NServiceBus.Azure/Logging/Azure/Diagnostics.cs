namespace NServiceBus.Azure
{
    using System.Configuration;

    public class Diagnostics : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString", IsRequired = true, DefaultValue = "UseDevelopmentStorage=true")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }

        [ConfigurationProperty("Level", IsRequired = false, DefaultValue = "Verbose")]
        public string Level
        {
            get { return (string)this["Level"]; }
            set { this["Level"] = value; }
        }

        [ConfigurationProperty("ScheduledTransferPeriod", IsRequired = false, DefaultValue = 10)]
        public int ScheduledTransferPeriod
        {
            get { return (int)this["ScheduledTransferPeriod"]; }
            set { this["ScheduledTransferPeriod"] = value; }
        }

        [ConfigurationProperty("EventLogs", IsRequired = false, DefaultValue = "")]
        public string EventLogs
        {
            get { return (string)this["EventLogs"]; }
            set { this["EventLogs"] = value; }
        }

        
    }
}