using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing
{
    public sealed class TopicPublishService<T> : ITopicPublishService<T>, IDisposable
        where T : new()
    {
        private readonly ITopicConfiguration _topicConfiguration;
        private readonly ISerializationService _serialisationService;
        private ITopicClient _topicClient;

        public TopicPublishService(ITopicConfiguration topicConfiguration, ISerializationService serialisationService)
        {
            _topicConfiguration = topicConfiguration;
            _serialisationService = serialisationService;
        }

        public async Task PublishAsync(T obj)
        {
            if (_topicClient == null)
            {
                var retryPolicy = new RetryExponential(
                        TimeSpan.FromSeconds(_topicConfiguration.MinimumBackoffSeconds),
                        TimeSpan.FromSeconds(_topicConfiguration.MaximumBackoffSeconds),
                        _topicConfiguration.MaximumRetryCount);

                _topicClient = new TopicClient(
                    _topicConfiguration.ConnectionString,
                    _topicConfiguration.TopicName,
                    retryPolicy);
            }

            await _topicClient.SendAsync(new Message(Encoding.UTF8.GetBytes(_serialisationService.Serialize(obj))));
        }

        public void Dispose()
        {
            _topicClient?.CloseAsync().GetAwaiter().GetResult();
        }
    }
}
