using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.DateTimeProvider.Interface;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.MessageLocking;
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

        private readonly IDateTimeProvider _dateTimeProvider;

        protected BaseSubscriptionService(ISerializationService serialisationService, ILogger logger, IDateTimeProvider dateTimeProvider)
        {
            _serialisationService = serialisationService;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
        }

        protected async Task ExceptionReceivedHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError("Failed to receive from message queue", arg.Exception);
        }

        protected async Task Handler(Message message, CancellationToken cancellationToken)
        {
            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken cancellationTokenOwned = cancellationTokenSource.Token;

            using (MessageLockManager messageLockManager = new MessageLockManager(
                _logger,
                _dateTimeProvider,
                _receiverClient,
                new LockMessage(message),
                cancellationTokenSource,
                cancellationTokenOwned))
            {
                try
                {
                    await messageLockManager.InitializeSession();

                    T obj = _serialisationService.Deserialize<T>(Encoding.UTF8.GetString(message.Body));

                    if (cancellationTokenOwned.IsCancellationRequested)
                    {
                        await messageLockManager.AbandonAsync(new TaskCanceledException());
                        return;
                    }

                    IQueueCallbackResult queueCallbackResult =
                        await _callback.Invoke(obj, message.UserProperties, cancellationTokenOwned);
                    if (queueCallbackResult.Result)
                    {
                        await messageLockManager.CompleteAsync();
                    }
                    else
                    {
                        await messageLockManager.AbandonAsync(queueCallbackResult.Exception);
                    }
                }
                catch (Exception ex)
                {
                    await messageLockManager.AbandonAsync(ex);
                    _logger.LogError("Error in queue handler", ex);
                }
            }
        }

        protected async Task UnsubscribeAndCleanupAsync()
        {
            await _receiverClient.CloseAsync();
            _receiverClient = null;
            _callback = null;
        }
    }
}
