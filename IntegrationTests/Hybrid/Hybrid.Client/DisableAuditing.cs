using NServiceBus;
using NServiceBus.Features;

namespace Hybrid.Client
{
    public class DisableAuditing : INeedInitialization
    {
        public void Init()
        {
            Feature.Disable<Audit>();
        }
    }
}