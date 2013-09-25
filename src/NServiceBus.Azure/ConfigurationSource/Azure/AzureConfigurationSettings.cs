namespace NServiceBus.Integration.Azure
{
    using Config;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class AzureConfigurationSettings : IAzureConfigurationSettings
    {
        public string GetSetting(string name)
        {
            if (!SafeRoleEnvironment.IsAvailable) return string.Empty;

            return RoleEnvironment.GetConfigurationSettingValue(name);
        }

        public bool TryGetSetting(string name, out string setting)
        {
            setting = null;

            if (!SafeRoleEnvironment.IsAvailable) return false;

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