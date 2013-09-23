namespace NServiceBus.Integration.Azure
{
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class AzureConfigurationSettings : IAzureConfigurationSettings
    {
        public string GetSetting(string name)
        {
            if (!RoleEnvironment.IsAvailable) return string.Empty;

            return RoleEnvironment.GetConfigurationSettingValue(name);
        }

        public bool TryGetSetting(string name, out string setting)
        {
            setting = string.Empty;

            if (!RoleEnvironment.IsAvailable) return false;

            try
            {
               setting = RoleEnvironment.GetConfigurationSettingValue(name);
                return true;
            }
            catch (RoleEnvironmentException)
            {
                return false;
            }
        }
    }
}