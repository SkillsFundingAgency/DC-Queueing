using ESFA.DC.Queueing.Interface.Configuration;
using FluentAssertions;
using Xunit;

namespace ESFA.DC.Queueing.Tests
{
    public sealed class TestConfiguration
    {
        [Fact]
        public void TestConfigurationObject()
        {
            IQueueConfiguration configuration = new QueueConfiguration("ConnectionString", "QueueName", 10, 20, 30, 40, 50);

            configuration.MaxConcurrentCalls.Should().Be(10);
            configuration.ConnectionString.Should().Be("ConnectionString");
            configuration.MaximumBackoffSeconds.Should().Be(30);
            configuration.MaximumRetryCount.Should().Be(40);
            configuration.MinimumBackoffSeconds.Should().Be(20);
            configuration.QueueName.Should().Be("QueueName");
            configuration.MaximumCallbackTimeoutMinutes.Should().Be(50);
        }
    }
}
