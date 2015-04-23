namespace NServiceBus.Integration.Azure
{
    public interface IAzureConfigurationSettings
    {
        string GetSetting(string name);
        bool TryGetSetting(string name, out string setting);
    }
}