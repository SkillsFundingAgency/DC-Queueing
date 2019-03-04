namespace ESFA.DC.Queueing.MessageLocking
{
    public sealed class MessageRenewalConfiguration
    {
        public MessageRenewalConfiguration(string url, string keyName, string key)
        {
            Url = url;
            KeyName = keyName;
            Key = key;
        }

        public string Url { get; }

        public string KeyName { get; }

        public string Key { get; }
    }
}
