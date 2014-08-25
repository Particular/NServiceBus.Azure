namespace NServiceBus.Azure.Transports.WindowsAzureStorageQueues
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using CircuitBreakers;
    using Logging;
    using NServiceBus.Transports;
    using Unicast.Transport;

    /// <summary>
    /// A polling implementation of <see cref="IDequeueMessages"/>.
    /// </summary>
    public class PollingDequeueStrategy : IDequeueMessages
    {
        readonly AzureMessageQueueReceiver messageReceiver;

        public PollingDequeueStrategy(AzureMessageQueueReceiver messageReceiver, CriticalError criticalError)
        {
            this.messageReceiver = messageReceiver;
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("AzureStoragePollingDequeueStrategy", TimeSpan.FromSeconds(30), ex => criticalError.Raise(string.Format("Failed to receive message from Azure Storage Queue."), ex));
        }

        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">Needs to be called by <see cref="IDequeueMessages"/> after the message has been processed regardless if the outcome was successful or not.</param>
        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;

            addressToPoll = address;
            settings = transactionSettings;
            transactionOptions = new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout };

            messageReceiver.Init(addressToPoll, settings.IsTransactional);
        }

        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel"/>.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Indicates the maximum concurrency level this <see cref="IDequeueMessages"/> is able to support.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            tokenSource = new CancellationTokenSource();

            for (var i = 0; i < maximumConcurrencyLevel; i++)
            {
                StartThread();
            }
        }

        /// <summary>
        /// Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();
        }

        void StartThread()
        {
            var token = tokenSource.Token;

            Task.Factory
                .StartNew(Action, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(t =>
                    {
                        t.Exception.Handle(ex =>
                            {
                                circuitBreaker.Failure(ex);
                                return true;
                            });

                        StartThread();
                    }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void Action(object obj)
        {
            var cancellationToken = (CancellationToken)obj;

            while (!cancellationToken.IsCancellationRequested)
            {
                Exception exception = null;
                TransportMessage message = null;

                try
                {
                    if (settings.IsTransactional)
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                        {
                            message = messageReceiver.Receive();

                            if (message != null)
                            {
                                if (tryProcessMessage(message))
                                {
                                    scope.Complete();
                                }
                            }
                        }
                    }
                    else
                    {
                        message = messageReceiver.Receive();

                        if (message != null)
                        {
                            tryProcessMessage(message);
                        }
                    }

                    circuitBreaker.Success();
                }
                catch (EnvelopeDeserializationFailed ex)
                {
                    //if we failed to deserialize the envlope there isn't much we can do so we just swallow the message to avoid a infinite loop
                    message = new TransportMessage(ex.FailedMessage.Id,new Dictionary<string, string>());
                    exception = ex;

                    Logger.Error("Failed to deserialize the envelope of the incoming message. Message will be discarded. MessageId: " + ex.FailedMessage.Id,exception);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    if (!cancellationToken.IsCancellationRequested && (message != null || exception != null))
                    {
                        endProcessMessage(message, exception);
                    }
                }
            }
        }

        readonly RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        Func<TransportMessage, bool> tryProcessMessage;
        CancellationTokenSource tokenSource;
        Address addressToPoll;
        TransactionSettings settings;
        TransactionOptions transactionOptions;
        Action<TransportMessage, Exception> endProcessMessage;
        static ILog Logger = LogManager.GetLogger(typeof (PollingDequeueStrategy));
    }
}