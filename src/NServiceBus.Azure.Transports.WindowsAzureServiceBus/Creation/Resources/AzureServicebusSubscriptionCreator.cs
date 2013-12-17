namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus.Messaging;

    public class AzureServicebusSubscriptionCreator : ICreateSubscriptions
    {
        public TimeSpan LockDuration { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public bool EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

        readonly ICreateNamespaceManagers createNamespaceManagers;

        public AzureServicebusSubscriptionCreator() : this(new CreatesNamespaceManagers())
        {
        }

        public AzureServicebusSubscriptionCreator(ICreateNamespaceManagers createNamespaceManagers)
        {
            this.createNamespaceManagers = createNamespaceManagers;
        }

        public void Create(Address topic, Type eventType, string subscriptionname)
        {
            var topicPath = topic.Queue;
            var namespaceClient = createNamespaceManagers.Create(topic.Machine);
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
                            EnableDeadLetteringOnFilterEvaluationExceptions = EnableDeadLetteringOnFilterEvaluationExceptions
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
            }
            else
            {
                throw new InvalidOperationException(string.Format("The topic that you're trying to subscribe to, {0}, doesn't exist", topicPath));
            }
        }

        public void Delete(Address topic, string subscriptionname)
        {
            var namespaceClient = createNamespaceManagers.Create(topic.Machine);
            if (namespaceClient.SubscriptionExists(topic.Queue, subscriptionname))
            {
                namespaceClient.DeleteSubscription(topic.Queue, subscriptionname);
            }
        }
    }
}