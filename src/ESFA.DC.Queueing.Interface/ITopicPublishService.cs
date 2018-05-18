using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ESFA.DC.Queueing.Interface
{
    public interface ITopicPublishService<in T>
        where T : new()
    {
        Task PublishAsync(T obj, IDictionary<string, object> properties, string messageLabel);
    }
}
