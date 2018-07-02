using System;

namespace ESFA.DC.Queueing.Interface
{
    public interface IQueueCallbackResult
    {
        bool Result { get; }

        Exception Exception { get; }
    }
}
