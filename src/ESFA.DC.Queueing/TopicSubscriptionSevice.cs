using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.DateTimeProvider.Interface;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.Interface.Configuration;
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
            ILogger logger,
            IDateTimeProvider dateTimeProvider)
            : base(serialisationService, logger, dateTimeProvider)
        {
            _topicConfiguration = topicConfiguration;
        }

        public void Subscribe(Func<T, IDictionary<string, object>, CancellationToken, Task<IQueueCallbackResult>> callback)
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
                AutoComplete = false,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(_topicConfiguration.MaximumCallbackTimeoutMinutes)
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