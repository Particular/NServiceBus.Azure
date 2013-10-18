namespace VideoStore.Operations
{
    using System;
    using System.Diagnostics;
    using VideoStore.Common;
    using VideoStore.Messages.RequestResponse;
    using NServiceBus;

    public class ProvisionDownloadHandler : IHandleMessages<ProvisionDownloadRequest>
    {
        public IBus Bus { get; set; }

        public void Handle(ProvisionDownloadRequest message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Trace.WriteLine(string.Format("Provision the videos and make the Urls available to the Content management for download ...[{0}] video(s) to provision", String.Join(", ", message.VideoIds)));

            Bus.Reply(new ProvisionDownloadResponse
                {
                    OrderNumber = message.OrderNumber,
                    VideoIds = message.VideoIds,
                    ClientId = message.ClientId
                });
        }
    }
}