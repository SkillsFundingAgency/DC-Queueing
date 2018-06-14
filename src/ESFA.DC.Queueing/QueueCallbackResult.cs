using System;
using ESFA.DC.Queueing.Interface;

namespace ESFA.DC.Queueing
{
    public sealed class QueueCallbackResult : IQueueCallbackResult
    {
        public QueueCallbackResult(bool result, Exception exception)
        {
            Result = result;
            Exception = exception;
        }

        public bool Result { get; }

        public Exception Exception { get; }
    }
}
