namespace ESFA.DC.Queueing.Interface.Configuration
{
    public interface IBaseConfiguration
    {
        string ConnectionString { get; }

        int MaxConcurrentCalls { get; }

        int MinimumBackoffSeconds { get; }

        int MaximumBackoffSeconds { get; }

        int MaximumRetryCount { get; }

        int MaximumCallbackTimeoutMinutes { get; }
    }
}
