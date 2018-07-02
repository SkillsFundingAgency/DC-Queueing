using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace ESFA.DC.Queueing
{
    public class BaseSubscriptionService<T>
    {
        protected Func<T, IDictionary<string, object>, CancellationToken, Task<IQueueCallbackResult>> _callback;

        protected IReceiverClient _receiverClient;

        private readonly ISerializationService _serialisationService;

        private readonly ILogger _logger;

        protected BaseSubscriptionService(ISerializationService serialisationService, ILogger logger)
        {
            _serialisationService = serialisationService;
            _logger = logger;
        }

        protected async Task ExceptionReceivedHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError("Failed to receive from Auditing message queue", arg.Exception);
        }

        protected async Task Handler(Message message, CancellationToken cancellationToken)
        {
            try
            {
                T obj = _serialisationService.Deserialize<T>(Encoding.UTF8.GetString(message.Body));

                if (cancellationToken.IsCancellationRequested)
                {
                    await _receiverClient.AbandonAsync(message.SystemProperties.LockToken);
                    return;
                }

                IQueueCallbackResult queueCallbackResult = await _callback.Invoke(obj, message.UserProperties, cancellationToken);
                if (queueCallbackResult.Result)
                {
                    await _receiverClient.CompleteAsync(message.SystemProperties.LockToken);
                }
                else
                {
                    await _receiverClient.AbandonAsync(message.SystemProperties.LockToken, GetProperties(message.UserProperties, queueCallbackResult.Exception));
                }
            }
            catch (Exception ex)
            {
                await _receiverClient.AbandonAsync(message.SystemProperties.LockToken, GetProperties(message.UserProperties, ex));
                _logger.LogError("Error in queue handler", ex);
            }
        }

        protected async Task UnsubscribeAndCleanupAsync()
        {
            await _receiverClient.CloseAsync();
            _receiverClient = null;
            _callback = null;
        }

        private IDictionary<string, object> GetProperties(
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
