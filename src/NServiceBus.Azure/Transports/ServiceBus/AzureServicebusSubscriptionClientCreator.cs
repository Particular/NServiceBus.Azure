namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Azure.Transports.ServiceBus;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServicebusSubscriptionClientCreator : ICreateSubscriptionClients
    {
        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        public TimeSpan LockDuration { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public bool EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

        public SubscriptionClient Create(Address address, Type eventType)
        {
            var topicPath = address.Queue;
            var subscriptionname = AzureServiceBusSubscriptionNameConvention.Create(eventType);
            return Create(eventType, topicPath, subscriptionname);
        }

        public SubscriptionClient Create(Type eventType, string topicPath, string subscriptionname)
        {
            if (NamespaceClient.TopicExists(topicPath))
            {
                try
                {
                    if (!NamespaceClient.SubscriptionExists(topicPath, subscriptionname))
                    {
                        var description = new SubscriptionDescription(topicPath, subscriptionname)
                        {
                            LockDuration = LockDuration,
                            RequiresSession = RequiresSession,
                            DefaultMessageTimeToLive = DefaultMessageTimeToLive,
                            EnableDeadLetteringOnMessageExpiration = EnableDeadLetteringOnMessageExpiration,
                            MaxDeliveryCount = MaxDeliveryCount,
                            EnableBatchedOperations = EnableBatchedOperations,
                            EnableDeadLetteringOnFilterEvaluationExceptions =
                                EnableDeadLetteringOnFilterEvaluationExceptions
                        };

                        if (eventType != null)
                        {
                            var filter = new ServicebusSubscriptionFilterBuilder().BuildFor(eventType);

                            NamespaceClient.CreateSubscription(description, new SqlFilter(filter));
                        }
                        else
                        {
                            NamespaceClient.CreateSubscription(description);
                        }
                    }
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // the queue already exists or another node beat us to it, which is ok
                }

                return Factory.CreateSubscriptionClient(topicPath, subscriptionname, ReceiveMode.PeekLock);
               
            }
           else
            {
                throw new InvalidOperationException(string.Format("The topic that you're trying to subscribe to, {0}, doesn't exist", topicPath));
            }
        }
    }
}