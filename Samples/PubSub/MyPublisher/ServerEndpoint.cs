using System;
using MyMessages;
using NServiceBus;

namespace MyPublisher
{
    public class ServerEndpoint : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("This will publish IEvent and EventMessage alternately.");
            Console.WriteLine("Press 'Enter' to publish a message.To exit, Ctrl + C");

            bool publishIEvent = true;
            while (Console.ReadLine() != null)
            {
                var eventMessage = publishIEvent ? Bus.CreateInstance<IMyEvent>() : new EventMessage();

                eventMessage.EventId = Guid.NewGuid();
                eventMessage.Time = DateTime.Now.Second > 30 ? (DateTime?)DateTime.Now : null;
                eventMessage.Duration = TimeSpan.FromSeconds(99999D);

                Bus.Publish(eventMessage);

                Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);
                Console.WriteLine("==========================================================================");

                publishIEvent = !publishIEvent;
            }
        }

        public void Stop()
        {

        }
    }
}