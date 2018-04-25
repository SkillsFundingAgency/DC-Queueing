using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing
{
    public sealed class QueueSubscriptionService<T> : IQueueSubscriptionService<T>
    {
        private readonly ILogger _logger;

        private readonly IQueueConfiguration _queueConfiguration;

        private readonly ISerializationService _serialisationService;

        private QueueClient _queueClient;

        private Func<T, CancellationToken, Task<bool>> _callback;

        public QueueSubscriptionService(IQueueConfiguration queueConfiguration, ISerializationService serialisationService, ILogger logger)
        {
            _queueConfiguration = queueConfiguration;
            _serialisationService = serialisationService;
            _logger = logger;
        }

        public void Subscribe(Func<T, CancellationToken, Task<bool>> callback)
        {
            if (_queueClient == null)
            {
                _queueClient = new QueueClient(_queueConfiguration.ConnectionString, _queueConfiguration.QueueName, ReceiveMode.PeekLock, new RetryExponential(TimeSpan.FromSeconds(_queueConfiguration.MinimumBackoffSeconds), TimeSpan.FromSeconds(_queueConfiguration.MaximumBackoffSeconds), _queueConfiguration.MaximumRetryCount));
            }

            MessageHandlerOptions messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = _queueConfiguration.MaxConcurrentCalls,
                AutoComplete = false
            };
            _queueClient.RegisterMessageHandler(Handler, messageHandlerOptions);
            _callback = callback;
        }

        public async Task UnsubscribeAsync()
        {
            await _queueClient.CloseAsync();
            _queueClient = null;
            _callback = null;
        }

        private async Task ExceptionReceivedHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError("Failed to receive from Auditing message queue", arg.Exception);
        }

        private async Task Handler(Message message, CancellationToken cancellationToken)
        {
            try
            {
                T obj = _serialisationService.Deserialize<T>(Encoding.UTF8.GetString(message.Body));

                if (await _callback.Invoke(obj, cancellationToken))
                {
                    await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            }
            catch (Exception ex)
            {
                await _queueClient.AbandonAsync(message.SystemProperties.LockToken);
                _logger.LogError("Error in queue handler", ex);
            }
        }
    }
}
