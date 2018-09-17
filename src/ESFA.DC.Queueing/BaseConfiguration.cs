using System;
using System.Collections.Generic;
using System.Text;
using ESFA.DC.Queueing.Interface.Configuration;

namespace ESFA.DC.Queueing
{
    public abstract class BaseConfiguration : IBaseConfiguration
    {
        protected BaseConfiguration(string connectionString, int maxConcurrentCalls, int minimumBackoffSeconds = 5, int maximumBackoffSeconds = 50, int maximumRetryCount = 10, int maximumCallbackTimeoutMinutes = 10)
        {
            ConnectionString = connectionString;
            MaxConcurrentCalls = maxConcurrentCalls;
            MinimumBackoffSeconds = minimumBackoffSeconds;
            MaximumBackoffSeconds = maximumBackoffSeconds;
            MaximumRetryCount = maximumRetryCount;
            MaximumCallbackTimeoutMinutes = maximumCallbackTimeoutMinutes;
        }

        public string ConnectionString { get; }

        public int MaxConcurrentCalls { get; }

        public int MinimumBackoffSeconds { get; }

        public int MaximumBackoffSeconds { get; }

        public int MaximumRetryCount { get; }

        public int MaximumCallbackTimeoutMinutes { get; }
    }
}
