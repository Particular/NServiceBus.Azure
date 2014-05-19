namespace NServiceBus.Config
{
    using Azure;

    public class SetDefaultConfigurationSource : IWantToRunBeforeConfiguration
    {
        public void Init(Configure config)
        {
            if (SafeRoleEnvironment.IsAvailable)
            {
                if (!IsHostedIn.ChildHostProcess())
                    Configure.Instance.AzureConfigurationSource();
            }
        }
    }
}