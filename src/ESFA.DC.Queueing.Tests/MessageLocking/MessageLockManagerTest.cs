using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface.Configuration;
using ESFA.DC.Queueing.MessageLocking;
using FluentAssertions;
using Microsoft.Azure.ServiceBus.Core;
using Moq;
using Xunit;

namespace ESFA.DC.Queueing.Tests.MessageLocking
{
    public sealed class MessageLockManagerTest
    {
        [Fact]
        public async Task TestTimeout()
        {
            string messageId = Guid.NewGuid().ToString();
            string lockToken = Guid.NewGuid().ToString();

            Mock<ILogger> loggerMock = new Mock<ILogger>();
            Mock<IReceiverClient> receiverClientMock = new Mock<IReceiverClient>();
            Mock<IBaseConfiguration> baseConfigurationMock = new Mock<IBaseConfiguration>();
            baseConfigurationMock.SetupGet(c => c.MaximumCallbackTimeSpan).Returns(new TimeSpan(0, 0, 1));

            LockMessage msg = new LockMessage(messageId, lockToken, new Dictionary<string, object>());

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            MessageLockManager messageLockManager = new MessageLockManager(loggerMock.Object, receiverClientMock.Object, baseConfigurationMock.Object, msg, null, cancellationTokenSource, cancellationToken);
            await messageLockManager.InitializeSession(TimeSpan.FromSeconds(1));

            await Task.Delay(TimeSpan.FromSeconds(4), CancellationToken.None);

            cancellationToken.IsCancellationRequested.Should().BeTrue();
        }
    }
}
