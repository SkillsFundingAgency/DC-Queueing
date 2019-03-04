using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.Interface.Configuration;
using ESFA.DC.Queueing.MessageLocking;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using ReceiveMode = Microsoft.Azure.ServiceBus.ReceiveMode;
using SubscriptionClient = Microsoft.Azure.ServiceBus.SubscriptionClient;

namespace ESFA.DC.Queueing
{
    public class TopicSubscriptionSevice<T> : BaseSubscriptionService<T>, ITopicSubscriptionService<T>
    {
        private readonly ITopicConfiguration _topicConfiguration;

        public TopicSubscriptionSevice(
            ITopicConfiguration topicConfiguration,
            ISerializationService serialisationService,
            ILogger logger)
            : base(serialisationService, logger, topicConfiguration, $"{topicConfiguration.TopicName}/subscriptions/{topicConfiguration.SubscriptionName}")
        {
            _topicConfiguration = topicConfiguration;
        }

        public void Subscribe(Func<T, IDictionary<string, object>, CancellationToken, Task<IQueueCallbackResult>> callback, CancellationToken cancellationToken)
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
                MaxAutoRenewDuration = _topicConfiguration.MaximumCallbackTimeSpan
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