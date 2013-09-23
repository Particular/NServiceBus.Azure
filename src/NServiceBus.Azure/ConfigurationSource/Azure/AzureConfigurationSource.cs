namespace NServiceBus.Integration.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Web.Configuration;
    using System.Web.Hosting;
    using Config.ConfigurationSource;

    public class AzureConfigurationSource : IConfigurationSource
    {
        static readonly IDictionary<string, object> ConfigurationCache = new ConcurrentDictionary<string, object>();

        readonly IAzureConfigurationSettings azureConfigurationSettings;
        readonly bool cache;

        public AzureConfigurationSource(IAzureConfigurationSettings configurationSettings, bool usecache = true)
        {
            azureConfigurationSettings = configurationSettings;
            cache = usecache;
        }

        public string ConfigurationPrefix { get; set; }

        T IConfigurationSource.GetConfiguration<T>()
        {
            var sectionName = typeof(T).Name;
            if (cache && ConfigurationCache.ContainsKey(sectionName))
            {
                return (T) ConfigurationCache[sectionName];
            }

            var section = GetConfigurationHandler()
                              .GetSection(sectionName) as T;

            foreach (var property in typeof(T).GetProperties().Where(x => x.DeclaringType == typeof(T)))
            {
                var adjustedPrefix = !string.IsNullOrEmpty(ConfigurationPrefix) ? ConfigurationPrefix + "." : string.Empty;

                string setting;

                if (!azureConfigurationSettings.TryGetSetting(adjustedPrefix + sectionName + "." + property.Name,out setting))
                {
                    continue;
                }
                if( section == null) section = new T();

                property.SetValue(section, Convert.ChangeType(setting, property.PropertyType), null);
            }

            if (cache)
            {
                ConfigurationCache[sectionName] = section;
            }

            return section;
        }

        private static Configuration GetConfigurationHandler()
        {
            if (IsWebsite()) return WebConfigurationManager.OpenWebConfiguration(HostingEnvironment.ApplicationVirtualPath);

            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        private static bool IsWebsite()
        {
            return HostingEnvironment.IsHosted;
        }
    }
}