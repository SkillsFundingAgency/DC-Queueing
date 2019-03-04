using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ESFA.DC.Logging.Interfaces;

namespace ESFA.DC.Queueing.MessageLocking
{
    public sealed class MessageRenewalService
    {
        private readonly MessageRenewalConfiguration _messageRenewalConfiguration;

        private readonly ILogger _logger;

        public MessageRenewalService(MessageRenewalConfiguration messageRenewalConfiguration, ILogger logger)
        {
            _messageRenewalConfiguration = messageRenewalConfiguration;
            _logger = logger;
        }

        public void RenewMessage(LockMessage lockMessage)
        {
            try
            {
                string url = $"{_messageRenewalConfiguration.Url}/messages/{lockMessage.MessageId}/{lockMessage.LockToken}";
                string token = GetSASToken(_messageRenewalConfiguration.Url, _messageRenewalConfiguration.KeyName, _messageRenewalConfiguration.Key);
                // _logger.LogInfo($"Renew message starting {url} with {token}");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("Authorization", token);
                request.ContentLength = 0;
                HttpWebResponse dataStream = (HttpWebResponse)request.GetResponse();
                _logger.LogInfo($"Renew message response {dataStream.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to renew message", ex);
            }
        }

        private static string GetSASToken(string resourceUri, string keyName, string key)
        {
            string expiry = GetExpiry();
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));

            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            string sasToken = string.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
            return sasToken;
        }

        private static string GetExpiry()
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Convert.ToString((int)sinceEpoch.TotalSeconds + 3600);
        }
    }
}
