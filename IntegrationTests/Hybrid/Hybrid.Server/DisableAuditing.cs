using NServiceBus;
using NServiceBus.Features;

namespace Hybrid.Server
{
    public class DisableAuditing : INeedInitialization
    {
        public void Init()
        {
            Feature.Disable<Audit>();
        }
    }
}