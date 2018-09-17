using System;
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
            var timespan = new TimeSpan(1, 1, 1);

            IQueueConfiguration configuration = new QueueConfiguration("ConnectionString", "QueueName", 10, 20, 30, 40, timespan);

            configuration.MaxConcurrentCalls.Should().Be(10);
            configuration.ConnectionString.Should().Be("ConnectionString");
            configuration.MaximumBackoffSeconds.Should().Be(30);
            configuration.MaximumRetryCount.Should().Be(40);
            configuration.MinimumBackoffSeconds.Should().Be(20);
            configuration.QueueName.Should().Be("QueueName");
            configuration.MaximumCallbackTimeSpan.Should().Be(timespan);
        }

        [Fact]
        public void QueueConfiguration_NullMaximumCallbackTimeout()
        {
            IQueueConfiguration configuration = new QueueConfiguration("ConnectionString", "QueueName", 10, 20, 30, 40, null);

            configuration.MaxConcurrentCalls.Should().Be(10);
            configuration.ConnectionString.Should().Be("ConnectionString");
            configuration.MaximumBackoffSeconds.Should().Be(30);
            configuration.MaximumRetryCount.Should().Be(40);
            configuration.MinimumBackoffSeconds.Should().Be(20);
            configuration.QueueName.Should().Be("QueueName");
            configuration.MaximumCallbackTimeSpan.Should().Be(new TimeSpan(0, 10, 0));
        }
    }
}
