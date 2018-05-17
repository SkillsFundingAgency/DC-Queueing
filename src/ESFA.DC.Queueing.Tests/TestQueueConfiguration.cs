﻿namespace ESFA.DC.Queueing.Tests
{
    public sealed class TestQueueConfiguration : QueueConfiguration
    {
        public TestQueueConfiguration(string connectionString, string queueName, int maxConcurrentCalls, string topicName = null, int minimumBackoffSeconds = 5, int maximumBackoffSeconds = 50, int maximumRetryCount = 10)
            : base(connectionString, queueName, maxConcurrentCalls, topicName, minimumBackoffSeconds, maximumBackoffSeconds, maximumRetryCount)
        {
        }
    }
}