namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using Microsoft.ServiceBus;
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

            var filter = "1=1";

            if (eventType != null)
            {
                filter = new ServicebusSubscriptionFilterBuilder().BuildFor(eventType);
            }

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

                        if (filter != string.Empty)
                        {
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
                catch (TimeoutException)
                {
                    // there is a chance that the timeout occurs, but the subscription is created still
                    // check for this
                    if (!namespaceClient.SubscriptionExists(topicPath, subscriptionname))
                        throw;
                }

                GuardAgainstSubscriptionReuseAcrossLogicalEndpoints(subscriptionname, namespaceClient, topicPath, filter);
            }
            else
            {
                throw new InvalidOperationException(string.Format("The topic that you're trying to subscribe to, {0}, doesn't exist", topicPath));
            }
        }

        static void GuardAgainstSubscriptionReuseAcrossLogicalEndpoints(string subscriptionname,
            NamespaceManager namespaceClient, string topicPath, string filter)
        {
            var rules = namespaceClient.GetRules(topicPath, subscriptionname);
            foreach (var rule in rules)
            {
                var sqlFilter = rule.Filter as SqlFilter;
                if (sqlFilter != null && sqlFilter.SqlExpression != filter)
                {
                    throw new SubscriptionAlreadyInUseException(
                        "Looks like this subscriptionname is already taken by another logical endpoint as the sql filter does not match the subscribed eventtype, please choose a different subscription name!");
                }
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