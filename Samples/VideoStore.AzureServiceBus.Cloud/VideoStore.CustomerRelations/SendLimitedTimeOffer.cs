namespace VideoStore.CustomerRelations
{
    using System;
    using System.Diagnostics;
    using Messages.Events;
    using NServiceBus;
    using VideoStore.Common;

    class SendLimitedTimeOffer : IHandleMessages<ClientBecamePreferred>
    {
        public void Handle(ClientBecamePreferred message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }
            Trace.WriteLine(string.Format("Handler WhenCustomerIsPreferredSendLimitedTimeOffer invoked for CustomerId: {0}", message.ClientId));
        }
    }
}
