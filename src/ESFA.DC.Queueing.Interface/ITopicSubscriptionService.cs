using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ESFA.DC.Queueing.Interface
{
    public interface ITopicSubscriptionService<T>
    {
        void Subscribe(Func<T, CancellationToken, Task<bool>> callback);

        Task UnsubscribeAsync();
    }
}
