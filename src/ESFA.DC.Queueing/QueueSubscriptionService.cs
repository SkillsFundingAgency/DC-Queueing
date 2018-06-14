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
    public sealed class QueueSubscriptionService<T> : BaseSubscriptionService<T>, IQueueSubscriptionService<T>
    {
        private readonly IQueueConfiguration _queueConfiguration;

        private readonly ISerializationService _serialisationService;

        private IQueueClient _queueClient;

        public QueueSubscriptionService(IQueueConfiguration queueConfiguration, ISerializationService serialisationService, ILogger logger)
        : base(logger)
        {
            _queueConfiguration = queueConfiguration;
            _serialisationService = serialisationService;
        }

        public void Subscribe(Func<T, CancellationToken, Task<IQueueCallbackResult>> callback)
        {
            if (_queueClient == null)
            {
                var retryExponential = new RetryExponential(
                    TimeSpan.FromSeconds(_queueConfiguration.MinimumBackoffSeconds),
                    TimeSpan.FromSeconds(_queueConfiguration.MaximumBackoffSeconds),
                    _queueConfiguration.MaximumRetryCount);

                _queueClient = new QueueClient(
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

            _queueClient.RegisterMessageHandler(Handler, messageHandlerOptions);
            _callback = callback;
        }

        public async Task UnsubscribeAsync()
        {
            await _queueClient.CloseAsync();
            _queueClient = null;
            _callback = null;
        }

        private async Task Handler(Message message, CancellationToken cancellationToken)
        {
            try
            {
                T obj = _serialisationService.Deserialize<T>(Encoding.UTF8.GetString(message.Body));

                if (cancellationToken.IsCancellationRequested)
                {
                    await _queueClient.AbandonAsync(message.SystemProperties.LockToken);
                    return;
                }

                IQueueCallbackResult queueCallbackResult = await _callback.Invoke(obj, cancellationToken);
                if (queueCallbackResult.Result)
                {
                    await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
                }
                else
                {
                    await _queueClient.AbandonAsync(message.SystemProperties.LockToken, GetProperties(message.UserProperties, queueCallbackResult.Exception));
                }
            }
            catch (Exception ex)
            {
                await _queueClient.AbandonAsync(message.SystemProperties.LockToken, GetProperties(message.UserProperties, ex));
                _logger.LogError("Error in queue handler", ex);
            }
        }
    }
}
