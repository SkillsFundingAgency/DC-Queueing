using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing.Interface.Configuration;
using Microsoft.Azure.ServiceBus.Core;

namespace ESFA.DC.Queueing.MessageLocking
{
    public sealed class MessageLockManager : IDisposable
    {
        private readonly ILogger _logger;

        private readonly IReceiverClient _receiverClient;

        private readonly IBaseConfiguration _subscriberConfiguration;

        private readonly LockMessage _message;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly CancellationToken _cancellationTokenSb;

        private readonly SemaphoreSlim _locker;

        private bool isMessageActioned;

        private Timer _timer;

        public MessageLockManager(ILogger logger, IReceiverClient receiverClient, IBaseConfiguration subscriberConfiguration, LockMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationTokenSB)
        {
            _logger = logger;
            _receiverClient = receiverClient;
            _subscriberConfiguration = subscriberConfiguration;
            _message = message;
            _cancellationTokenSource = cancellationTokenSource;
            _cancellationTokenSb = cancellationTokenSB;
            _locker = new SemaphoreSlim(1, 1);
            isMessageActioned = false;
        }

        public void Dispose()
        {
            try
            {
                AbandonAsync().Wait(_cancellationTokenSb);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to dispose message lock manager: {ex}");
            }
        }

        /// <summary>
        /// Must be called to setup the message lock manager timer functionality.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task InitializeSession()
        {
            TimeSpan renewInterval = new TimeSpan(
                (long)Math.Round(
                    _subscriberConfiguration.MaximumCallbackTimeSpan.Ticks * 0.9,
                    0,
                    MidpointRounding.AwayFromZero));

            if (renewInterval.TotalMilliseconds < 0)
            {
                _logger.LogError($"Invalid message lock renewel value {renewInterval} for message {_message.MessageId}. Rejecting message.");
                await DoActionAsync(MessageAction.Abandon);
                return;
            }

            _logger.LogInfo($"Message {_message.MessageId} will be given {renewInterval.Minutes} minutes to execute before automatic cancellation.");

            _timer = new Timer(Callback, null, renewInterval, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Completes the message if the message has not previously been actioned and cancellation has not been requested.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task CompleteAsync()
        {
            await DoActionAsync(MessageAction.Complete);
        }

        /// <summary>
        /// Abandons the message if the message has not previously been actioned and cancellation has not been requested.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task AbandonAsync(Exception ex = null)
        {
            await DoActionAsync(MessageAction.Abandon, ex);
        }

        /// <summary>
        /// Dead letters the message if the message has not previously been actioned and cancellation has not been requested.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task DeadLetterAsync(Exception ex = null)
        {
            await DoActionAsync(MessageAction.DeadLetter, ex);
        }

        private void Callback(object state)
        {
            _logger.LogWarning($"Message {_message.MessageId} did not process in expected time, it will be abandoned and work cancelled.");
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            AbandonAsync().Wait(CancellationToken.None); // Timer will be disposed
            _cancellationTokenSource.Cancel(); // Cancel at the end so that we don't prevent processing of the message
        }

        private async Task DoActionAsync(MessageAction messageAction, Exception ex = null)
        {
            try
            {
                await _locker.WaitAsync(_cancellationTokenSb);

                if (!CanAction())
                {
                    return;
                }

                switch (messageAction)
                {
                    case MessageAction.Complete:
                        await _receiverClient.CompleteAsync(_message.LockToken);
                        break;
                    case MessageAction.Abandon:
                        await _receiverClient.AbandonAsync(_message.LockToken, GetProperties(_message.UserProperties, ex));
                        break;
                    case MessageAction.DeadLetter:
                        await _receiverClient.DeadLetterAsync(_message.LockToken, GetProperties(_message.UserProperties, ex));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(messageAction), messageAction, null);
                }

                isMessageActioned = true;
            }
            catch (Exception ex2)
            {
                _logger.LogError("Failed to action a message", ex2);
            }
            finally
            {
                _locker.Release();
            }
        }

        private bool CanAction()
        {
            if (_cancellationTokenSb.IsCancellationRequested)
            {
                return false;
            }

            if (isMessageActioned)
            {
                return false;
            }

            _timer?.Dispose();
            _timer = null;

            if (_message == null)
            {
                return false;
            }

            return true;
        }

        private IDictionary<string, object> GetProperties(
            IDictionary<string, object> messageUserProperties,
            Exception ex)
        {
            if (ex == null)
            {
                return new Dictionary<string, object>();
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
