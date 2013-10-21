using System.Linq;

namespace VideoStore.Sales
{
    using System;
    using System.Diagnostics;
    using Common;
    using Messages.Commands;
    using Messages.Events;
    using NServiceBus;
    using NServiceBus.Saga;

    public class ProcessOrderSaga : Saga<ProcessOrderSaga.OrderData>,
                                    IAmStartedByMessages<SubmitOrder>,
                                    IHandleMessages<CancelOrder>,
                                    IHandleTimeouts<ProcessOrderSaga.BuyersRemorseIsOver>
    {
        public void Handle(SubmitOrder message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Data.OrderNumber = message.OrderNumber;
            Data.VideoIds = string.Join(",", message.VideoIds);
            Data.ClientId = message.ClientId;

            RequestTimeout(TimeSpan.FromSeconds(20), new BuyersRemorseIsOver());
            Trace.WriteLine(string.Format("Starting cool down period for order #{0}.", Data.OrderNumber));
        }

        public void Timeout(BuyersRemorseIsOver state)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Bus.Publish<OrderAccepted>(e =>
                {
                    e.OrderNumber = Data.OrderNumber;
                    e.VideoIds =  Data.VideoIds.Split(',');
                    e.ClientId = Data.ClientId;
                });

            MarkAsComplete();

            Trace.WriteLine(string.Format("Cooling down period for order #{0} has elapsed.", Data.OrderNumber));
        }

        public void Handle(CancelOrder message)
        {
            if (DebugFlagMutator.Debug)
            {
                   Debugger.Break();
            }

            MarkAsComplete();

            Bus.Publish(Bus.CreateInstance<OrderCancelled>(o =>
                {
                    o.OrderNumber = message.OrderNumber;
                    o.ClientId = message.ClientId;
                }));

            Trace.WriteLine(string.Format("Order #{0} was cancelled.", message.OrderNumber));
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<SubmitOrder>(m => m.OrderNumber)
                .ToSaga(s=>s.OrderNumber);
            ConfigureMapping<CancelOrder>(m => m.OrderNumber)
                .ToSaga(s=>s.OrderNumber);
        }

        // Note: azure table storage is not a relational database and is limited in it's support for complex saga data structures
        // If you need more complexity, you may want to swap it out for sql server (against sql azure)
        public class OrderData : ContainSagaData
        {
            //[Unique] // not supported by the azure table storage saga persister at the moment
            public int OrderNumber { get; set; }
            
            //string[] is not supported by the azure table storage saga persister at the moment
            //public string[] VideoIds { get; set; }
            public string VideoIds { get; set; }

            public string ClientId { get; set; }
        }

        public class BuyersRemorseIsOver
        {
        }
    }

    
}