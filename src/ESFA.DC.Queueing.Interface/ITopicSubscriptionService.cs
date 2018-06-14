using System;
using System.Threading;
using System.Threading.Tasks;

namespace ESFA.DC.Queueing.Interface
{
    public interface ITopicSubscriptionService<T>
    {
        void Subscribe(Func<T, CancellationToken, Task<IQueueCallbackResult>> callback);

        Task UnsubscribeAsync();
    }
}
