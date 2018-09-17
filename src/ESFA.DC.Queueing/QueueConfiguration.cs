using ESFA.DC.Queueing.Interface.Configuration;

namespace ESFA.DC.Queueing
{
    public class QueueConfiguration : BaseConfiguration, IQueueConfiguration
    {
        public QueueConfiguration(string connectionString, string queueName, int maxConcurrentCalls, int minimumBackoffSeconds = 5, int maximumBackoffSeconds = 50, int maximumRetryCount = 10, int maximumCallbackTimeoutMinutes = 10)
            : base(connectionString, maxConcurrentCalls, minimumBackoffSeconds, maximumBackoffSeconds, maximumRetryCount, maximumCallbackTimeoutMinutes)
        {
            QueueName = queueName;
        }

        public string QueueName { get; }
    }
}
