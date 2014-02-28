using System;
using System.Collections.Generic;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Azure.Transports.WindowsAzureServiceBus
{
    using System.Collections;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CircuitBreakers;
    using NServiceBus.Transports;
    using Unicast.Transport;

    /// <summary>
    /// Azure service bus implementation if <see cref="IDequeueMessages" />.
    /// </summary>
    public class AzureServiceBusDequeueStrategy : IDequeueMessages
    {
        private Address address;
        private TransactionSettings settings;
        private Func<TransportMessage, bool> tryProcessMessage;
        private Action<TransportMessage, Exception> endProcessMessage;
        private TransactionOptions transactionOptions;
        private readonly Queue pendingMessages = Queue.Synchronized(new Queue());
        private readonly IList<INotifyReceivedMessages> notifiers = new List<INotifyReceivedMessages>();
        private CancellationTokenSource tokenSource;
        private readonly CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));
        
        private const int PeekInterval = 50;
        private const int MaximumWaitTimeWhenIdle = 1000;
        private int timeToDelayNextPeek;

        private static int maximumConcurrencyLevel;

        /// <summary>
        /// 
        /// </summary>
        public Func<INotifyReceivedMessages> CreateNotifier = () =>
            {
                var notifier = Configure.Instance.Builder.Build<AzureServiceBusQueueNotifier>();
                notifier.BatchSize = maximumConcurrencyLevel;
                return notifier;
            };

        
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
            AzureServiceBusDequeueStrategy.maximumConcurrencyLevel = maximumConcurrencyLevel;

            CreateAndStartNotifier();
            
            tokenSource = new CancellationTokenSource();

            for (int i = 0; i < maximumConcurrencyLevel; i++)
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
                                    circuitBreaker.Execute(() => Configure.Instance.RaiseCriticalError("Failed to receive message!" /* from?*/, ex));
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

                if (!RenewLockIfNeeded(brokeredMessage)) continue;

                var transportMessage = BrokeredMessageConverter.ToTransportMessage(brokeredMessage);

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
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    endProcessMessage(transportMessage, exception);
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
            foreach (var notifier in notifiers)
            {
                notifier.Stop();
            }

            notifiers.Clear();

            tokenSource.Cancel();
        }

        void CreateAndStartNotifier()
        {
            var notifier = CreateNotifier();
            TrackNotifier(address, notifier);
        }

        public void TrackNotifier(Address address, INotifyReceivedMessages notifier)
        {
            notifier.Start(address, EnqueueMessage);
            notifiers.Add(notifier);
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

        public void RemoveNotifier(Address publisherAddress)
        {
            var toRemove = notifiers.Cast<AzureServiceBusQueueNotifier>()
                                    .FirstOrDefault(notifier => notifier.Address == publisherAddress);

            if (toRemove == null) return;

            toRemove.Stop();
            notifiers.Remove(toRemove);
        }

    }


}