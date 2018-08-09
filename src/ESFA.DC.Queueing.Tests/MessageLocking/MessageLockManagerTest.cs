using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.DateTimeProvider.Interface;
using ESFA.DC.Logging.Interfaces;
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
            DateTime now = DateTime.UtcNow;
            DateTime nowFuture = now.AddSeconds(5);
            string messageId = Guid.NewGuid().ToString();
            string lockToken = Guid.NewGuid().ToString();
            bool abandoned = false;

            Mock<ILogger> loggerMock = new Mock<ILogger>();
            Mock<IReceiverClient> receiverClientMock = new Mock<IReceiverClient>();
            Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>();
            receiverClientMock.Setup(x => x.AbandonAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                .Callback<string, IDictionary<string, object>>(
                    (l, d) => { abandoned = true; }).Returns(Task.CompletedTask);
            dateTimeProvider.Setup(x => x.GetNowUtc()).Returns(now);

            LockMessage msg = new LockMessage(nowFuture, messageId, lockToken, new Dictionary<string, object>());

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            MessageLockManager messageLockManager = new MessageLockManager(loggerMock.Object, dateTimeProvider.Object, receiverClientMock.Object, msg, cancellationTokenSource, cancellationToken);
            await messageLockManager.InitializeSession();

            await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

            abandoned.Should().BeTrue();
            cancellationToken.IsCancellationRequested.Should().BeTrue();
        }
    }
}
