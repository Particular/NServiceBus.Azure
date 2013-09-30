using NServiceBus;
using NServiceBus.Features;

namespace Hybrid.Server
{
    public class DisableAuditing : IWantCustomInitialization
    {
        public void Init()
        {
            Feature.Disable<Audit>();
        }
    }
}