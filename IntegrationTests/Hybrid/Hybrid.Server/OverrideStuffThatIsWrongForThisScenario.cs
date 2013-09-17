using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Saga;
using NServiceBus.Timeout.Core;

namespace Hybrid.Server
{
    public class OverrideStuffThatIsWrongForThisScenario : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.RavenSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseRavenTimeoutPersister());
        }
    }
}