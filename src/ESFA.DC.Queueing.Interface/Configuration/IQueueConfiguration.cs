namespace ESFA.DC.Queueing.Interface.Configuration
{
    public interface IQueueConfiguration : IBaseConfiguration
    {
        string QueueName { get; }
    }
}
