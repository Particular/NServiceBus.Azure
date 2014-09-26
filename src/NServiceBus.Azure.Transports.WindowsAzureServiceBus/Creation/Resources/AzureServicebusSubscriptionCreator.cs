namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    class AzureServicebusSubscriptionCreator : ICreateSubscriptions
    {
        public TimeSpan LockDuration { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public bool EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

        ICreateNamespaceManagers createNamespaceManagers;
        Configure config;

        static Dictionary<string, bool> rememberTopicExistence = new Dictionary<string, bool>();
        static Dictionary<string, bool> rememberSubscriptionExistence = new Dictionary<string, bool>();
        static object TopicExistenceLock = new Object();
        static object SubscriptionExistenceLock = new Object();

        public AzureServicebusSubscriptionCreator(ICreateNamespaceManagers createNamespaceManagers, Configure config)
        {
            this.createNamespaceManagers = createNamespaceManagers;
            this.config = config;
        }

        public SubscriptionDescription Create(Address topic, Type eventType, string subscriptionname)
        {
            var topicPath = topic.Queue;
            var namespaceClient = createNamespaceManagers.Create(topic.Machine);

            var filter = "1=1";

            if (eventType != null)
            {
                filter = new ServicebusSubscriptionFilterBuilder().BuildFor(eventType);
            }

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

            if (config.CreateQueues())
            {
                if (TopicExists(namespaceClient, topicPath))
                {
                    try
                    {
                        if (!SubscriptionExists(namespaceClient, topicPath, subscriptionname))
                        {

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
            return description;
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
            if (SubscriptionExists(namespaceClient, topic.Queue, subscriptionname))
            {
                namespaceClient.DeleteSubscription(topic.Queue, subscriptionname);
            }
        }

        bool TopicExists(NamespaceManager namespaceClient, string topicpath)
        {
            var key = topicpath;
            bool exists;
            if (!rememberTopicExistence.ContainsKey(key))
            {
                lock (TopicExistenceLock)
                {
                    exists = namespaceClient.TopicExists(key);
                    rememberTopicExistence[key] = exists;
                }
            }
            else
            {
                exists = rememberTopicExistence[key];
            }

            return exists;
        }

        bool SubscriptionExists(NamespaceManager namespaceClient, string topicpath, string subscriptionname)
        {
            var key = topicpath + subscriptionname;
            bool exists;
            if (!rememberSubscriptionExistence.ContainsKey(key))
            {
                lock (SubscriptionExistenceLock)
                {
                    exists = namespaceClient.SubscriptionExists(topicpath, subscriptionname);
                    rememberSubscriptionExistence[key] = exists;
                }
            }
            else
            {
                exists = rememberSubscriptionExistence[key];
            }

            return exists;
        }
    }
}