using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.Interface.Configuration;
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
            : base(serialisationService, logger, queueConfiguration)
        {
            _queueConfiguration = queueConfiguration;
        }

        public void Subscribe(Func<T, IDictionary<string, object>, CancellationToken, Task<IQueueCallbackResult>> callback, CancellationToken cancellationToken)
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
                AutoComplete = false,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(_queueConfiguration.MaximumCallbackTimeoutMinutes)
            };

            _cancellationTokenExt = cancellationToken;
            _callback = callback;
            _receiverClient.RegisterMessageHandler(Handler, messageHandlerOptions);
        }

        public async Task UnsubscribeAsync()
        {
            await UnsubscribeAndCleanupAsync();
        }
    }
}