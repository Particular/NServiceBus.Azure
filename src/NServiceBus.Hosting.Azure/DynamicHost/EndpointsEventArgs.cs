using System;
using System.Collections.Generic;

namespace NServiceBus.Hosting
{
    internal class EndpointsEventArgs : EventArgs
    {
        public IEnumerable<EndpointToHost> Endpoints { get; set; }
    }
}