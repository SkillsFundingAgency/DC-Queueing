using ESFA.DC.Queueing.Interface;
using FluentAssertions;
using Xunit;

namespace ESFA.DC.Queueing.Tests
{
    public sealed class TestConfiguration
    {
        [Fact]
        public void TestConfigurationObject()
        {
            IQueueConfiguration configuration = new TestQueueConfiguration("ConnectionString", "QueueName", 10, "TopicName", "SubscriptionName", 100, 1000, 10000);

            configuration.MaxConcurrentCalls.Should().Be(10);
            configuration.ConnectionString.Should().Be("ConnectionString");
            configuration.MaximumBackoffSeconds.Should().Be(1000);
            configuration.MaximumRetryCount.Should().Be(10000);
            configuration.MinimumBackoffSeconds.Should().Be(100);
            configuration.QueueName.Should().Be("QueueName");
            configuration.TopicName.Should().Be("TopicName");
        }
    }
}
