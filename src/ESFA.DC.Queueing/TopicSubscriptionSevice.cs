using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing
{
    public class TopicSubscriptionSevice<T> : ITopicSubscriptionService<T>
    {
        private readonly IQueueConfiguration _queueConfiguration;
        private readonly ISerializationService _serialisationService;
        private readonly ILogger _logger;
        private ISubscriptionClient _subscriptionClient;

        private Func<T, CancellationToken, Task<bool>> _callback;

        public TopicSubscriptionSevice(IQueueConfiguration queueConfiguration, ISerializationService serialisationService, ILogger logger)
        {
            _queueConfiguration = queueConfiguration;
            _serialisationService = serialisationService;
            _logger = logger;
        }

        public void Subscribe(Func<T, CancellationToken, Task<bool>> callback)
        {
            if (_subscriptionClient == null)
            {
                var retryExponential = new RetryExponential(
                    TimeSpan.FromSeconds(_queueConfiguration.MinimumBackoffSeconds),
                    TimeSpan.FromSeconds(_queueConfiguration.MaximumBackoffSeconds),
                    _queueConfiguration.MaximumRetryCount);

                _subscriptionClient = new SubscriptionClient(
                    _queueConfiguration.ConnectionString,
                    _queueConfiguration.TopicName,
                    "test",
                    ReceiveMode.PeekLock,
                    retryExponential);
            }

            MessageHandlerOptions messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = _queueConfiguration.MaxConcurrentCalls,
                AutoComplete = false
            };

            _subscriptionClient.RegisterMessageHandler(Handler, messageHandlerOptions);
            _callback = callback;
        }

        public async Task UnsubscribeAsync()
        {
            await _subscriptionClient.CloseAsync();
            _subscriptionClient = null;
            _callback = null;
        }

        private async Task Handler(Message message, CancellationToken cancellationToken)
        {
            try
            {
                T obj = _serialisationService.Deserialize<T>(Encoding.UTF8.GetString(message.Body));

                if (cancellationToken.IsCancellationRequested)
                {
                    await _subscriptionClient.AbandonAsync(message.SystemProperties.LockToken);
                    return;
                }

                if (await _callback.Invoke(obj, cancellationToken))
                {
                    await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            }
            catch (Exception ex)
            {
                await _subscriptionClient.AbandonAsync(message.SystemProperties.LockToken);
                _logger.LogError("Error in Topic handler", ex);
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError("Failed to receive from Auditing message queue", arg.Exception);
            return Task.CompletedTask;
        }
    }
}
