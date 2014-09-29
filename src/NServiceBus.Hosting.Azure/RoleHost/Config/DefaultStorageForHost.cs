namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    public class DefaultStorageForHost : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            //config.Settings.SetDefault("Persistence", typeof(AzureStorage));

            //var selectedTransport = config.Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            //InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.AzureSagaPersister());
            //InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseAzureTimeoutPersister());

            //if (selectedTransport == null || !selectedTransport.HasNativePubSubSupport)
            //{
            //    InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.AzureSubscriptionStorage());
            //}
            
        }
    }
}