using NServiceBus;
using NServiceBus.Features;

namespace Hybrid.Client
{
    public class DisableAuditing : IWantCustomInitialization
    {
        public void Init()
        {
            Feature.Disable<Audit>();
        }
    }
}