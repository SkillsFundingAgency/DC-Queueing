using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ESFA.DC.Queueing.Interface
{
    public interface IBaseSubscriptionService<T>
    {
        void Subscribe(Func<T, IDictionary<string, object>, CancellationToken, Task<IQueueCallbackResult>> callback, CancellationToken cancellationToken);

        Task UnsubscribeAsync();
    }
}
