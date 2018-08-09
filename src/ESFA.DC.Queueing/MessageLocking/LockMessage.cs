using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing.MessageLocking
{
    public sealed class LockMessage
    {
        public LockMessage(Message message)
        : this(message.SystemProperties.LockedUntilUtc, message.MessageId, message.SystemProperties.LockToken, message.UserProperties)
        {
        }

        public LockMessage(DateTime lockedUntilUtc, string messageId, string lockToken, IDictionary<string, object> userProperties)
        {
            LockedUntilUtc = lockedUntilUtc;
            MessageId = messageId;
            LockToken = lockToken;
            UserProperties = userProperties;
        }

        public IDictionary<string, object> UserProperties { get; }

        public string LockToken { get; }

        public string MessageId { get; }

        public DateTime LockedUntilUtc { get; }
    }
}
