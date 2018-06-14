using System;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing
{
    public class TopicSubscriptionSevice<T> : BaseSubscriptionService<T>, ITopicSubscriptionService<T>
    {
        private readonly ITopicConfiguration _topicConfiguration;

        public TopicSubscriptionSevice(
            ITopicConfiguration topicConfiguration,
            ISerializationService serialisationService,
            ILogger logger)
            : base(serialisationService, logger)
        {
            _topicConfiguration = topicConfiguration;
        }

        public void Subscribe(Func<T, CancellationToken, Task<IQueueCallbackResult>> callback)
        {
            if (_receiverClient == null)
            {
                var retryExponential = new RetryExponential(
                    TimeSpan.FromSeconds(_topicConfiguration.MinimumBackoffSeconds),
                    TimeSpan.FromSeconds(_topicConfiguration.MaximumBackoffSeconds),
                    _topicConfiguration.MaximumRetryCount);

                _receiverClient = new SubscriptionClient(
                    _topicConfiguration.ConnectionString,
                    _topicConfiguration.TopicName,
                    _topicConfiguration.SubscriptionName,
                    ReceiveMode.PeekLock,
                    retryExponential);
            }

            MessageHandlerOptions messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = _topicConfiguration.MaxConcurrentCalls,
                AutoComplete = false
            };

            _receiverClient.RegisterMessageHandler(Handler, messageHandlerOptions);
            //_callback = callback;
        }

        public async Task UnsubscribeAsync()
        {
            await UnsubscribeAndCleanupAsync();
        }
    }
}