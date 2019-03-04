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

        private readonly MessageRenewalConfiguration _messageRenewalConfiguration;

        protected BaseSubscriptionService(
            ISerializationService serialisationService,
            ILogger logger,
            IBaseConfiguration configuration,
            string entityPath)
        {
            _serialisationService = serialisationService;
            _logger = logger;
            _configuration = configuration;
            _messageRenewalConfiguration = GetRenewUrl(configuration.ConnectionString, entityPath);
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
                new MessageRenewalService(_messageRenewalConfiguration, _logger),
                cancellationTokenSource,
                cancellationTokenSB))
            {
                IQueueCallbackResult queueCallbackResult = null;
                Exception exception = null;

                try
                {
                    if (!await messageLockManager.InitializeSession())
                    {
                        await messageLockManager.AbandonAsync();
                        return;
                    }

                    T obj = _serialisationService.Deserialize<T>(Encoding.UTF8.GetString(message.Body));

                    if (cancellationTokenOwned.IsCancellationRequested)
                    {
                        await messageLockManager.AbandonAsync();
                        return;
                    }

                    queueCallbackResult = await _callback.Invoke(obj, message.UserProperties, cancellationTokenOwned);
                    exception = queueCallbackResult.Exception;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    _logger.LogError("Error in queue handler", ex);
                }

                if (queueCallbackResult?.Result == true)
                {
                    await messageLockManager.CompleteAsync();
                }
                else
                {
                    await messageLockManager.AbandonAsync(exception);
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

        private static MessageRenewalConfiguration GetRenewUrl(string connectionString, string entityPath)
        {
            string[] tokens = connectionString.Split(';');
            string url = "https://" + tokens[0].Substring("Endpoint=sb://".Length) + entityPath;
            string keyName = tokens[1].Substring("SharedAccessKeyName=".Length);
            string key = tokens[2].Substring("SharedAccessKey=".Length);
            return new MessageRenewalConfiguration(url, keyName, key);
        }
    }
}
