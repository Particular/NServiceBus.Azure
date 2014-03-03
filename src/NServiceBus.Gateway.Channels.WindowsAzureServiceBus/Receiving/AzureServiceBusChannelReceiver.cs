namespace NServiceBus.Gateway.Channels.WindowsAzureServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using CircuitBreakers;
    using Microsoft.ServiceBus.Messaging;

    [ChannelType("AzureServiceBus")]
    public class AzureServiceBusChannelReceiver : IChannelReceiver
    {
        private CancellationTokenSource tokenSource;
        private readonly CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));

        private readonly Queue pendingMessages = Queue.Synchronized(new Queue());
        private readonly IList<INotifyReceivedMessages> notifiers = new List<INotifyReceivedMessages>();

        private TransactionOptions transactionOptions;

        private const int PeekInterval = 50;
        private const int MaximumWaitTimeWhenIdle = 1000;
        private int timeToDelayNextPeek;
        private int maximumConcurrencyLevel = 0;

        public AzureServiceBusChannelReceiver()
        {
            transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable, 
                Timeout = TimeSpan.FromMinutes(5) //todo: get this from somewhere once ioc enabled
            };
        }

        public void Dispose()
        {
        }

        public void Start(string address, int numberOfWorkerThreads)
        {
            tokenSource = new CancellationTokenSource();

            CreateAndStartNotifier(address, numberOfWorkerThreads);

            maximumConcurrencyLevel += numberOfWorkerThreads;

            for (int i = 0; i < numberOfWorkerThreads; i++)
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

                if (!RenewLockIfNeeded(brokeredMessage)) continue;

                if (Transaction.Current != null)
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                    {
                        Transaction.Current.EnlistVolatile(new ReceiveResourceManager(brokeredMessage), EnlistmentOptions.None);

                        OnDataReceived(brokeredMessage);
                            
                        scope.Complete();
                    }
                }
                else
                {
                    OnDataReceived(brokeredMessage);

                    brokeredMessage.SafeComplete();
                }
            }
        }

        void CreateAndStartNotifier(string address, int numberOfWorkerThreads)
        {
            var notifier = CreateNotifier(numberOfWorkerThreads);
            TrackNotifier(address, notifier);
        }

        void TrackNotifier(string address, INotifyReceivedMessages notifier)
        {
            notifier.Start(address, EnqueueMessage);
            notifiers.Add(notifier);
        }

        void EnqueueMessage(BrokeredMessage brokeredMessage)
        {
            while (pendingMessages.Count > 2 * maximumConcurrencyLevel) { Thread.Sleep(10); }

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

        public event EventHandler<DataReceivedOnChannelArgs> DataReceived;


        private Func<int, INotifyReceivedMessages> CreateNotifier = (int numberOfWorkerThreads) =>
        {
            //todo: IOC this Configure.Instance.Builder.Build<AzureServiceBusQueueNotifier>();
            var notifier = new AzureServiceBusQueueNotifier(); 
            notifier.QueueClientCreator = new AzureServicebusGatewayQueueClientCreator(
                new AzureServiceBusGatewayQueueCreator(),
                new CreatesMessagingFactories());
            notifier.BatchSize = numberOfWorkerThreads;
            return notifier;
        };

        void OnDataReceived(BrokeredMessage message)
        {
            var streamToReturn = message.GetBody<Stream>();
            IDictionary<string, string> headers = message.Properties.ToDictionary(k => k.Key, k => k.Value.ToString());

            var pos = streamToReturn.Position;
            var r = new StreamReader(streamToReturn);
            var str = r.ReadToEnd();
            streamToReturn.Position = pos;

            Debug.WriteLine(str);
          
            //streamToReturn.Position = 0;

            if (DataReceived != null)
            {
                
                DataReceived(this, new DataReceivedOnChannelArgs()
                {
                    Headers = headers,
                    Data = streamToReturn
                });
            }
        }
    }
}
