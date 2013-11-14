namespace NServiceBus.Config
{
    using Azure;

    public class SetDefaultConfigurationSource : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            if (SafeRoleEnvironment.IsAvailable)
            {
                if (!IsHostedIn.ChildHostProcess())
                    Configure.Instance.AzureConfigurationSource();
            }
        }
    }
}