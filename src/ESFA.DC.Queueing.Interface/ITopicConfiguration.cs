using System;
using System.Collections.Generic;
using System.Text;

namespace ESFA.DC.Queueing.Interface
{
    public interface ITopicConfiguration
    {
        string ConnectionString { get; }

        string TopicName { get; }

        string SubscriptionName { get; }

        int MaxConcurrentCalls { get; }

        int MinimumBackoffSeconds { get; }

        int MaximumBackoffSeconds { get; }

        int MaximumRetryCount { get; }
    }
}
