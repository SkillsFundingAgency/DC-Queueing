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
                    _queueConfiguration.sub
                    ReceiveMode.PeekLock,
                    retryExponential);
            }
        }

        public Task UnsubscribeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
