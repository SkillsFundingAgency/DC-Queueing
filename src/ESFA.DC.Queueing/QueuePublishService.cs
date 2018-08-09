using System;
using System.Text;
using System.Threading.Tasks;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.Interface.Configuration;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing
{
    public sealed class QueuePublishService<T> : IQueuePublishService<T>, IDisposable
        where T : new()
    {
        private readonly IQueueConfiguration _queueConfiguration;

        private readonly ISerializationService _serialisationService;

        private IQueueClient _queueClient;

        public QueuePublishService(IQueueConfiguration queueConfiguration, ISerializationService serialisationService)
        {
            _queueConfiguration = queueConfiguration;
            _serialisationService = serialisationService;
        }

        public async Task PublishAsync(T obj)
        {
            if (_queueClient == null)
            {
                _queueClient = new QueueClient(_queueConfiguration.ConnectionString, _queueConfiguration.QueueName, ReceiveMode.PeekLock, new RetryExponential(TimeSpan.FromSeconds(_queueConfiguration.MinimumBackoffSeconds), TimeSpan.FromSeconds(_queueConfiguration.MaximumBackoffSeconds), _queueConfiguration.MaximumRetryCount));
            }

            await _queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes(_serialisationService.Serialize(obj))));
        }

        public void Dispose()
        {
            _queueClient?.CloseAsync().GetAwaiter().GetResult();
        }
    }
}
