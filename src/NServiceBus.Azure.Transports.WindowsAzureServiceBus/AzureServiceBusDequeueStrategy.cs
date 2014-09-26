using System;
using System.Collections.Generic;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using CircuitBreakers;
    using NServiceBus.Transports;
    using Unicast.Transport;

    /// <summary>
    /// Azure service bus implementation if <see cref="IDequeueMessages" />.
    /// </summary>
    internal class AzureServiceBusDequeueStrategy : IDequeueMessages
    {
        ITopology topology;
        private Address address;
        private TransactionSettings settings;
        private Func<TransportMessage, bool> tryProcessMessage;
        private Action<TransportMessage, Exception> endProcessMessage;
        private TransactionOptions transactionOptions;
        private Queue pendingMessages = Queue.Synchronized(new Queue());
        private IDictionary<string, INotifyReceivedBrokeredMessages> notifiers = new Dictionary<string, INotifyReceivedBrokeredMessages>();
        private CancellationTokenSource tokenSource;
        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        
        private const int PeekInterval = 50;
        private const int MaximumWaitTimeWhenIdle = 1000;
        private int timeToDelayNextPeek;
        private int maximumConcurrencyLevel;

        public AzureServiceBusDequeueStrategy(ITopology topology, CriticalError criticalError)
        {
            this.topology = topology;
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("AzureStoragePollingDequeueStrategy", TimeSpan.FromSeconds(30), ex => criticalError.Raise(string.Format("Failed to receive message from Azure ServiceBus."), ex));
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
            this.maximumConcurrencyLevel = maximumConcurrencyLevel;

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


        private void TryProcessMessage(object obj)
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

                        brokeredMessage.SafeComplete(); 
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

            notifier.Start(EnqueueMessage);
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
            while (pendingMessages.Count > 2 * maximumConcurrencyLevel){Thread.Sleep(10);}

            try
            {
                if (brokeredMessage.LockedUntilUtc <= DateTime.UtcNow) { return; }
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
    }


}