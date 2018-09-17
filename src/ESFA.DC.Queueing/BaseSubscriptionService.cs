using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.Interface.Configuration;
using ESFA.DC.Queueing.MessageLocking;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace ESFA.DC.Queueing
{
    public abstract class BaseSubscriptionService<T>
    {
        protected Func<T, IDictionary<string, object>, CancellationToken, Task<IQueueCallbackResult>> _callback;

        protected CancellationToken _cancellationTokenExt;

        protected IReceiverClient _receiverClient;

        private readonly ISerializationService _serialisationService;

        private readonly ILogger _logger;

        private readonly IBaseConfiguration _configuration;

        protected BaseSubscriptionService(ISerializationService serialisationService, ILogger logger, IBaseConfiguration configuration)
        {
            _serialisationService = serialisationService;
            _logger = logger;
            _configuration = configuration;
        }

        protected async Task ExceptionReceivedHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError("Failed to receive from message queue", arg.Exception);
        }

        protected async Task Handler(Message message, CancellationToken cancellationTokenSB)
        {
            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSB, _cancellationTokenExt);
            CancellationToken cancellationTokenOwned = cancellationTokenSource.Token;

            using (MessageLockManager messageLockManager = new MessageLockManager(
                _logger,
                _receiverClient,
                _configuration,
                new LockMessage(message),
                cancellationTokenSource,
                cancellationTokenSB))
            {
                try
                {
                    await messageLockManager.InitializeSession();

                    T obj = _serialisationService.Deserialize<T>(Encoding.UTF8.GetString(message.Body));

                    if (cancellationTokenOwned.IsCancellationRequested)
                    {
                        await messageLockManager.AbandonAsync();
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
            try
            {
                await _receiverClient.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to close receiver client", ex);
            }

            _receiverClient = null;
            _callback = null;
        }
    }
}
