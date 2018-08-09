namespace ESFA.DC.Queueing.Interface.Configuration
{
    public interface ITopicConfiguration : IBaseConfiguration
    {
        string TopicName { get; }

        string SubscriptionName { get; }
    }
}
