using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using Microsoft.Azure.ServiceBus;

namespace ESFA.DC.Queueing
{
    public class BaseSubscriptionService<T>
    {
        protected readonly ILogger _logger;

        protected Func<T, CancellationToken, Task<IQueueCallbackResult>> _callback;

        protected BaseSubscriptionService(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task ExceptionReceivedHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError("Failed to receive from Auditing message queue", arg.Exception);
        }

        protected IDictionary<string, object> GetProperties(
            IDictionary<string, object> messageUserProperties,
            Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            if (messageUserProperties.TryGetValue("Exceptions", out var exceptions))
            {
                exceptions = $"{exceptions}:{ex.GetType().Name}";
            }
            else
            {
                exceptions = ex.GetType().Name;
            }

            return new Dictionary<string, object>
            {
                { "Exceptions", exceptions }
            };
        }
    }
}
