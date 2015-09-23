namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using Microsoft.ServiceBus.Messaging;
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using CircuitBreakers;
    using Logging;
    using NServiceBus.Transports;
    using Unicast.Transport;

    /// <summary>
    /// Azure service bus implementation if <see cref="IDequeueMessages" />.
    /// </summary>
    class AzureServiceBusDequeueStrategy : IDequeueMessages
    {
        ITopology topology;
        readonly CriticalError criticalError;
        Address address;
        TransactionSettings settings;
        Func<TransportMessage, bool> tryProcessMessage;
        Action<TransportMessage, Exception> endProcessMessage;
        TransactionOptions transactionOptions;
        Queue pendingMessages = Queue.Synchronized(new Queue());
        IDictionary<string, INotifyReceivedBrokeredMessages> notifiers = new Dictionary<string, INotifyReceivedBrokeredMessages>();
        CancellationTokenSource tokenSource;
        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;

        ILog logger = LogManager.GetLogger(typeof(AzureServiceBusDequeueStrategy));
        
        const int PeekInterval = 50;
        const int MaximumWaitTimeWhenIdle = 1000;
        int timeToDelayNextPeek;

        public AzureServiceBusDequeueStrategy(ITopology topology, CriticalError criticalError)
        {
            this.topology = topology;
            this.criticalError = criticalError;
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("AzureStoragePollingDequeueStrategy", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to receive message from Azure ServiceBus.", ex));
        }
        
        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">Needs to be called by <see cref="IDequeueMessages"/> after the message has been processed regardless if the outcome was successful or not.</param>
        public virtual void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
            settings = transactionSettings;
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;
            this.address = address;

            transactionOptions = new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout };
        }

        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel"/>.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Indicates the maximum concurrency level this <see cref="IDequeueMessages"/> is able to support.</param>
        public virtual void Start(int maximumConcurrencyLevel)
        {
            CreateAndStartNotifier();
            
            tokenSource = new CancellationTokenSource();

            for (var i = 0; i < maximumConcurrencyLevel; i++)
            {
                StartThread();
            }
        }

        void StartThread()
        {
            var token = tokenSource.Token;

            Task.Factory
                .StartNew(TryProcessMessage, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            t.Exception.Handle(ex =>
                                {
                                    circuitBreaker.Failure(ex);
                                    return true;
                                });
                        }

                        StartThread();
                    }, TaskContinuationOptions.OnlyOnFaulted);
        }


        void TryProcessMessage(object obj)
        {
            var cancellationToken = (CancellationToken)obj;

            while (!cancellationToken.IsCancellationRequested)
            {
                 BrokeredMessage brokeredMessage = null;

                 if (pendingMessages.Count > 0) brokeredMessage = pendingMessages.Dequeue() as BrokeredMessage;
                
                 if (brokeredMessage == null)
                 {
                     if (timeToDelayNextPeek < MaximumWaitTimeWhenIdle) timeToDelayNextPeek += PeekInterval;

                     Thread.Sleep(timeToDelayNextPeek);
                     continue;
                 }

                 timeToDelayNextPeek = 0;
                 Exception exception = null;

                // due to clock drift we may receive messages that aren't due yet according to our clock, let's put this back
                if (brokeredMessage.ScheduledEnqueueTimeUtc > DateTime.UtcNow) 
                {
                    pendingMessages.Enqueue(brokeredMessage);
                    continue;
                }


                if (!RenewLockIfNeeded(brokeredMessage)) continue;

                var transportMessage = brokeredMessage.ToTransportMessage();

                try
                {
                    if (settings.IsTransactional)
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                        {
                            Transaction.Current.EnlistVolatile(new ReceiveResourceManager(brokeredMessage), EnlistmentOptions.None);

                            if (transportMessage != null)
                            {
                                if (tryProcessMessage(transportMessage))
                                {
                                    scope.Complete();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (transportMessage != null)
                        {
                            tryProcessMessage(transportMessage);
                        }
                    }

                    circuitBreaker.Success();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    if (!cancellationToken.IsCancellationRequested && (transportMessage != null || exception != null))
                    {
                        endProcessMessage(transportMessage, exception);
                    }
                }
            }
        }

        static bool RenewLockIfNeeded(BrokeredMessage brokeredMessage)
        {
            try
            {
                if (brokeredMessage.LockedUntilUtc <= DateTime.UtcNow) return false;

                if (brokeredMessage.LockedUntilUtc <= DateTime.UtcNow.AddSeconds(10))
                {
                    try
                    {
                        brokeredMessage.RenewLock();
                    }
                    catch (MessageLockLostException)
                    {
                        return false;
                    }
                    catch (SessionLockLostException)
                    {
                        return false;
                    }
                    catch (TimeoutException)
                    {
                        return false;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // if the message was received without a peeklock mechanism you're not allowed to call LockedUntilUtc
                // sadly enough I can't find a public property that checks who the receiver was or if the locktoken has been set
                // those are internal to the sdk
            }
            
            return true;
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public virtual void Stop()
        {
            foreach (var notifier in notifiers.Values)
            {
                notifier.Stop();
            }

            notifiers.Clear();

            tokenSource.Cancel();
        }

        void CreateAndStartNotifier()
        {
            var notifier = topology.GetReceiver(address);
            TrackNotifier(null, address, notifier);
        }

        public void TrackNotifier(Type eventType, Address original, INotifyReceivedBrokeredMessages notifier)
        {
            var key = CreateKeyFor(eventType, original);
            if (notifiers.ContainsKey(key)) return;

            notifier.Start(EnqueueMessage, ErrorDequeueingBatch);
            notifiers.Add(key, notifier);
        }

        public void RemoveNotifier(Type eventType, Address original)
        {
            var key = CreateKeyFor(eventType, original);
            if (!notifiers.ContainsKey(key)) return;
            var toRemove = notifiers[key];
            toRemove.Stop();
            notifiers.Remove(key);
        }

        public INotifyReceivedBrokeredMessages GetNotifier(Type eventType, Address original)
        {
            var key = CreateKeyFor(eventType, original);
            return !notifiers.ContainsKey(key) ? null : notifiers[key];
        }

        void EnqueueMessage(BrokeredMessage brokeredMessage)
        {
           // while (pendingMessages.Count > 2 * maximumConcurrencyLevel){Thread.Sleep(10);}

            try
            {
                if (brokeredMessage.LockedUntilUtc <= DateTime.UtcNow)
                {
                    logger.Warn("Brokered message lock expired, this could be due to multiple reasons. One of the most common is a mismatch between the lock duration, batch size and processing speed of your handlers. This condition can also happen when there is clock skew between the client and the azure servicebus service.");

                    return;
                }
            }
            catch (InvalidOperationException)
            {
               // if the message was received without a peeklock mechanism you're not allowed to call LockedUntilUtc
               // sadly enough I can't find a public property that checks who the receiver was or if the locktoken has been set
               // those are internal to the sdk
            }

            pendingMessages.Enqueue(brokeredMessage);
        }

       

        string CreateKeyFor(Type eventType, Address original)
        {
            var key = original.ToString();
            if (eventType != null)
            {
                key += eventType.FullName;
            }
            return key;
        }
        void ErrorDequeueingBatch(Exception ex)
        {
            criticalError.Raise("Fatal messaging exception occured on the broker while dequeueing batch.", ex);
        }
    }
}