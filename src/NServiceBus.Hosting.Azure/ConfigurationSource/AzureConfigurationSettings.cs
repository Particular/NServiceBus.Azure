namespace NServiceBus.Integration.Azure
{
    using Config;
    
    public class AzureConfigurationSettings : IAzureConfigurationSettings
    {
        public string GetSetting(string name)
        {
            if (!SafeRoleEnvironment.IsAvailable) return string.Empty;

            return SafeRoleEnvironment.GetConfigurationSettingValue(name);
        }

        public bool TryGetSetting(string name, out string setting)
        {
            setting = null;

            if (!SafeRoleEnvironment.IsAvailable) return false;

            return SafeRoleEnvironment.TryGetConfigurationSettingValue(name, out setting);
        }
    }
}