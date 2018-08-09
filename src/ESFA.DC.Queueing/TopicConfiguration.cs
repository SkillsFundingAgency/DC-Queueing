using ESFA.DC.Queueing.Interface.Configuration;

namespace ESFA.DC.Queueing
{
    public abstract class TopicConfiguration : ITopicConfiguration
    {
        protected TopicConfiguration(string connectionString, string topicName, string subscriptionName, int maxConcurrentCalls, int minimumBackoffSeconds = 5, int maximumBackoffSeconds = 50, int maximumRetryCount = 10, int maximumCallbackTimeoutMinutes = 10)
        {
            MaxConcurrentCalls = maxConcurrentCalls;
            ConnectionString = connectionString;
            TopicName = topicName;
            SubscriptionName = subscriptionName;
            MinimumBackoffSeconds = minimumBackoffSeconds;
            MaximumBackoffSeconds = maximumBackoffSeconds;
            MaximumRetryCount = maximumRetryCount;
            MaximumCallbackTimeoutMinutes = maximumCallbackTimeoutMinutes;
        }

        public string ConnectionString { get; }

        public string TopicName { get; }

        public string SubscriptionName { get; }

        public int MaxConcurrentCalls { get; }

        public int MinimumBackoffSeconds { get; }

        public int MaximumBackoffSeconds { get; }

        public int MaximumRetryCount { get; }

        public int MaximumCallbackTimeoutMinutes { get; }
    }
}
