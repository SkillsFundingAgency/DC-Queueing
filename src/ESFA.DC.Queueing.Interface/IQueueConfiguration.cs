namespace ESFA.DC.Queueing.Interface
{
    public interface IQueueConfiguration
    {
        string ConnectionString { get; }

        string QueueName { get; }

        string TopicName { get; }
    }
}
