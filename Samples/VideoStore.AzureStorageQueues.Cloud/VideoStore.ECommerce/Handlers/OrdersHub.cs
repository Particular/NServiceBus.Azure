namespace VideoStore.ECommerce.Handlers
{
    using System.Threading;
    using Microsoft.AspNet.SignalR;
    using VideoStore.Messages.Commands;
    using NServiceBus;
    using System.Diagnostics;

    public class OrdersHub : Hub
    {
        private static int orderNumber;

        public void Join(string clientId)
        {
            Groups.Add(Context.ConnectionId, clientId);
        }

        public void CancelOrder(int order, string clientId, bool debug)
        {
            var command = new CancelOrder
            {
                ClientId = clientId,
                OrderNumber = order
            };

            MvcApplication.Bus.SetMessageHeader(command, "Debug", debug.ToString());

            MvcApplication.Bus.Send(command);
        }

        public void PlaceOrder(string[] videoIds, string clientId, bool debug)
        {
            if (debug)
            {
                Debugger.Break();
            }

            var command = new SubmitOrder
            {
                ClientId = clientId,
                OrderNumber = Interlocked.Increment(ref orderNumber),
                VideoIds = videoIds,
                EncryptedCreditCardNumber = "4000 0000 0000 0008", // This property will be encrypted. Therefore when viewing the message in the queue, the actual values will not be shown. 
                EncryptedExpirationDate = "10/13" // This property will be encrypted.
            };

            MvcApplication.Bus.SetMessageHeader(command, "Debug", debug.ToString());

            MvcApplication.Bus.Send(command);
        }
    }
}