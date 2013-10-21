namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;
    using NServiceBus.Azure.Transports.ServiceBus;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServicebusSubscriptionClientCreator : ICreateSubscriptionClients
    {
        public TimeSpan LockDuration { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public bool EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

        public SubscriptionClient Create(Address address, Type eventType)
        {
            var subscriptionname = AzureServiceBusSubscriptionNamingConvention.Apply(eventType);
            return Create(eventType, address, subscriptionname);
        }

        public SubscriptionClient Create(Type eventType, Address topic, string subscriptionname)
        {
            var topicPath = topic.Queue;
            var namespaceClient = new CreatesNamespaceManagers().Create(topic.Machine);
            if (namespaceClient.TopicExists(topicPath))
            {
                try
                {
                    

                    if (!namespaceClient.SubscriptionExists(topicPath, subscriptionname))
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

                            namespaceClient.CreateSubscription(description, new SqlFilter(filter));
                        }
                        else
                        {
                            namespaceClient.CreateSubscription(description);
                        }
                    }
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    // the queue already exists or another node beat us to it, which is ok
                }

                var factory = new CreatesMessagingFactories().Create(topic.Machine);
                return factory.CreateSubscriptionClient(topicPath, subscriptionname, ReceiveMode.PeekLock);
               
            }
           else
            {
                throw new InvalidOperationException(string.Format("The topic that you're trying to subscribe to, {0}, doesn't exist", topicPath));
            }
        }
    }
}