using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;

namespace SmsControl.Services
{
    public class SmsService
    {
        private readonly int _maxPerNumber;
        private readonly int _maxPerAccount;
        private ConcurrentDictionary<string, int> _numberUsage;
        private ConcurrentDictionary<string, DateTime> _activePhoneNumbers;
       
        private static readonly Lock _sendMessageLock = new();
        private static readonly Lock _resetLimitLock = new();
        private static readonly Lock _removeNumberLock = new();

        public SmsService(IConfiguration configuration)
        {
            _maxPerNumber = configuration.GetValue<int>("SmsControl:MaxMessagesPerNumberPerSecond");
            _maxPerAccount = configuration.GetValue<int>("SmsControl:MaxMessagesPerAccountPerSecond");
             _numberUsage = [];
            _activePhoneNumbers = [];
        }

        public bool CanSendMessage(string phoneNumber, out string message)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || !phoneNumber.All(char.IsDigit)) 
            {
                message = "Invalid phone number";
                return false;
            }
            lock (_sendMessageLock)
            {
                _numberUsage.TryGetValue(phoneNumber, out int currentNumberUsage);

                if (currentNumberUsage >= _maxPerNumber)
                {
                    message = "Limit exceeded for this phone number";
                    return false;
                }

                var accountUsage = _numberUsage.Values.Sum();
                if (accountUsage >= _maxPerAccount)
                {
                    message = "Limit exceeded for the entire account";
                    return false;
                }

                // Increment counters
                _numberUsage[phoneNumber] = currentNumberUsage + 1;

                // Update last activity timestamp
                _activePhoneNumbers.AddOrUpdate(phoneNumber, DateTime.Now, (k,v) => DateTime.Now);

                message = "SMS message allowed";
                return true;
            }
        }

        public bool ResetLimit(out string message)
        {
            if (_numberUsage == null ) {
                message = "Internal error ";
                    return false;
            }

            lock (_resetLimitLock)
            {
                _numberUsage.Clear();
                _activePhoneNumbers.Clear();
            }

            message = "SMS limit reset.";
            return true;
        }

        public ConcurrentDictionary<string, int> GetNumberUsage() {
            return _numberUsage;
        }
       
        public ConcurrentDictionary<string, DateTime> GetActivePhoneNumbers() {
            return _activePhoneNumbers;
        }
        public void RemoveExpiredNumbers(List<string> expiredNumbers) {
            lock (_removeNumberLock)
            {
                foreach (var number in expiredNumbers) {
                    _activePhoneNumbers.TryRemove(number, out _);
                    _numberUsage.TryRemove(number, out _);
                }
            };
        }
    }
}
