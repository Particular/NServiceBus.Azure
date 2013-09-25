using System;
using NServiceBus;
using NServiceBus.Features;

namespace Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            BootstrapNServiceBus();

            Console.WriteLine("Press enter to stop receiving");
            Console.ReadLine();
        }

        private static void BootstrapNServiceBus()
        {
            Configure.Transactions.Enable();
            Configure.Serialization.Binary();
            Feature.Disable<Audit>();

            Configure.With()
               .DefaultBuilder()
               .AzureMessageQueue()
               .AzureDataBus()
               .UnicastBus()
                    .LoadMessageHandlers()
               .CreateBus()
               .Start();
        }
    }
}
