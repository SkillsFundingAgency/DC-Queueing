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

        private readonly MessageRenewalService _messageRenewalService;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly CancellationToken _cancellationTokenSb;

        private readonly SemaphoreSlim _lockerSingleAction;

        private readonly object _lockerTimerStop;

        private readonly ManualResetEvent _lockerTimerDead;

        private bool _isMessageActioned;

        private Timer _timer;

        private DateTime _cancelDateTimeUtc;

        public MessageLockManager(
            ILogger logger,
            IReceiverClient receiverClient,
            IBaseConfiguration subscriberConfiguration,
            LockMessage message,
            MessageRenewalService messageRenewalService,
            CancellationTokenSource cancellationTokenSource,
            CancellationToken cancellationTokenSB)
        {
            _logger = logger;
            _receiverClient = receiverClient;
            _subscriberConfiguration = subscriberConfiguration;
            _message = message;
            _messageRenewalService = messageRenewalService;
            _cancellationTokenSource = cancellationTokenSource;
            _cancellationTokenSb = cancellationTokenSB;
            _lockerSingleAction = new SemaphoreSlim(1, 1);
            _isMessageActioned = false;
            _lockerTimerStop = new object();
            _lockerTimerDead = new ManualResetEvent(false);
        }

        public void Dispose()
        {
            try
            {
                // Stop and dispose the timer
                StopTimer();

                // Abandon the message if no other action was performed at this point!
                AbandonAsync().Wait(_cancellationTokenSb);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to dispose message lock manager", ex);
            }
        }

        /// <summary>
        /// Must be called to setup the message lock manager timer functionality.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task<bool> InitializeSession()
        {
            TimeSpan renewInterval = new TimeSpan(
                (long)Math.Round(
                    _subscriberConfiguration.MaximumCallbackTimeSpan.Ticks * 0.9,
                    0,
                    MidpointRounding.AwayFromZero));

            if (renewInterval.TotalMilliseconds < 0)
            {
                _logger.LogError($"Invalid message lock renewal value {renewInterval} for message {_message.MessageId}. Rejecting message.");
                return false;
            }

            _cancelDateTimeUtc = DateTime.UtcNow.AddMilliseconds(renewInterval.TotalMilliseconds);

            _logger.LogInfo($"Message {_message.MessageId} will be given {renewInterval.Minutes} minutes {renewInterval.Seconds} seconds to execute before automatic cancellation.");

            _timer = new Timer(Callback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(-1));
            return true;
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
            lock (_lockerTimerStop)
            {
                if (_lockerTimerDead.WaitOne(0))
                {
                    return;
                }

                if (DateTime.UtcNow >= _cancelDateTimeUtc)
                {
                    _logger.LogWarning($"Message {_message.MessageId} did not process in expected time, it will be abandoned and work cancelled.");
                    _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                    _cancellationTokenSource.Cancel(); // Cancel at the end so that we don't prevent processing of the message
                    return;
                }

                _messageRenewalService?.RenewMessage(_message);
                _timer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(-1));
            }
        }

        private async Task DoActionAsync(MessageAction messageAction, Exception ex = null)
        {
            try
            {
                await _lockerSingleAction.WaitAsync(_cancellationTokenSb);

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

                _isMessageActioned = true;
            }
            catch (Exception ex2)
            {
                _logger.LogError("Failed to action a message", ex2);
            }
            finally
            {
                _lockerSingleAction.Release();
            }
        }

        private bool CanAction()
        {
            if (_cancellationTokenSb.IsCancellationRequested)
            {
                return false;
            }

            if (_isMessageActioned)
            {
                return false;
            }

            StopTimer();

            if (_message == null)
            {
                return false;
            }

            return true;
        }

        private void StopTimer()
        {
            lock (_lockerTimerStop)
            {
                _lockerTimerDead.Set();
                _timer?.Dispose(); // May already be disposed
                _timer = null;
            }
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
