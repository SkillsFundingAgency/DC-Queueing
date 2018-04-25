using System.Threading.Tasks;

namespace ESFA.DC.Queueing.Interface
{
    public interface IQueuePublishService<in T>
        where T : new()
    {
        Task PublishAsync(T obj);
    }
}
