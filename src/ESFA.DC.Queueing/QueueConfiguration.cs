using ESFA.DC.Queueing.Interface;

namespace ESFA.DC.Queueing
{
    public abstract class QueueConfiguration : IQueueConfiguration
    {
        protected QueueConfiguration(string connectionString, string queueName, int maxConcurrentCalls, string topicName = null, string subscriptionName = null, int minimumBackoffSeconds = 5, int maximumBackoffSeconds = 50, int maximumRetryCount = 10)
        {
            ConnectionString = connectionString;
            QueueName = queueName;
            MaxConcurrentCalls = maxConcurrentCalls;
            TopicName = topicName;
            MinimumBackoffSeconds = minimumBackoffSeconds;
            MaximumBackoffSeconds = maximumBackoffSeconds;
            MaximumRetryCount = maximumRetryCount;
            SubscriptionName = subscriptionName;
        }

        public string ConnectionString { get; }

        public string QueueName { get; }

        public string TopicName { get; }

        public string SubscriptionName { get; }

        public int MaxConcurrentCalls { get; }

        public int MinimumBackoffSeconds { get; }

        public int MaximumBackoffSeconds { get; }

        public int MaximumRetryCount { get; }
    }
}
