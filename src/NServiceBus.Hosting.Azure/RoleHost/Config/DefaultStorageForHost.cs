namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    using Config;
    using Saga;
    using Settings;
    using Timeout.Core;
    using Transports;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class DefaultStorageForHost : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            var selectedTransport = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.AzureSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseAzureTimeoutPersister());

            if (selectedTransport == null || selectedTransport is AzureStorageQueue)
            {
                InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.AzureSubscriptionStorage());
            }
            
        }
    }
}