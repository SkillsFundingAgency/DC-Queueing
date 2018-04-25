using System;
using System.Threading;
using System.Threading.Tasks;

namespace ESFA.DC.Queueing.Interface
{
    public interface IQueueSubscriptionService<T>
    {
        void Subscribe(Func<T, CancellationToken, Task<bool>> callback);

        Task UnsubscribeAsync();
    }
}
