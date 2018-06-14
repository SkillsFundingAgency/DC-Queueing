using System;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing
{
    public sealed class QueueSubscriptionService<T> : BaseSubscriptionService<T>, IQueueSubscriptionService<T>
    {
        private readonly IQueueConfiguration _queueConfiguration;

        public QueueSubscriptionService(
            IQueueConfiguration queueConfiguration,
            ISerializationService serialisationService,
            ILogger logger)
            : base(serialisationService, logger)
        {
            _queueConfiguration = queueConfiguration;
        }

        public void Subscribe(Func<T, CancellationToken, Task<IQueueCallbackResult>> callback)
        {
            if (_receiverClient == null)
            {
                var retryExponential = new RetryExponential(
                    TimeSpan.FromSeconds(_queueConfiguration.MinimumBackoffSeconds),
                    TimeSpan.FromSeconds(_queueConfiguration.MaximumBackoffSeconds),
                    _queueConfiguration.MaximumRetryCount);

                _receiverClient = new QueueClient(
                    _queueConfiguration.ConnectionString,
                    _queueConfiguration.QueueName,
                    ReceiveMode.PeekLock,
                    retryExponential);
            }

            MessageHandlerOptions messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = _queueConfiguration.MaxConcurrentCalls,
                AutoComplete = false
            };

            _receiverClient.RegisterMessageHandler(Handler, messageHandlerOptions);
            _callback = callback;
        }

        public async Task UnsubscribeAsync()
        {
            await UnsubscribeAndCleanupAsync();
        }
    }
}