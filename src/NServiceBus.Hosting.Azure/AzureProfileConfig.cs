namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Configuration section for Azure host.
    /// </summary>
    public class AzureProfileConfig : ConfigurationSection
    {
        /// <summary>
        /// A comma separated list of profile names
        /// </summary>
        [ConfigurationProperty("Profiles", IsRequired = false)]
        public string Profiles
        {
            get
            {
                return this["Profiles"] as string;
            }
            set
            {
                this["Profiles"] = value;
            }
        }
    }
}
