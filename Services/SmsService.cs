using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.Serialization;

namespace SmsControl.Services
{
    public class SmsService
    {
        private readonly int _maxPerNumber;
        private readonly int _maxPerAccount;
        private ConcurrentDictionary<string, int> _numberUsage;
        private ConcurrentDictionary<string, List<DateTime>> _numberRecords;
        private static readonly Lock _sendMessageLock = new();
        private static readonly Lock _resetLimitLock = new();
        private static readonly Lock _removeNumberLock = new();
        private static readonly string[] FormatDate = ["yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss"];

        public SmsService(IConfiguration configuration)
        {
            _maxPerNumber = configuration.GetValue<int>("SmsControl:MaxMessagesPerNumber");
            _maxPerAccount = configuration.GetValue<int>("SmsControl:MaxMessagesPerAccount");
             _numberUsage = [];
            _numberRecords = [];
        }

        public bool CanSendMessage(string? phoneNumber, out string message)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || !phoneNumber.All(char.IsDigit)) 
            {
                message = "Invalid phone number";
                return false;
            }
            phoneNumber = phoneNumber.Trim();

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

                // Add timestamp to current number record
                if (_numberRecords.TryGetValue(phoneNumber, out List<DateTime> currentNumberRecords))
                {
                    currentNumberRecords.Add(DateTime.Now);
                    _numberRecords[phoneNumber] = currentNumberRecords;
                } else
                {
                    _numberRecords[phoneNumber] = [DateTime.Now];
                }

                message = "SMS message allowed";
                return true;
            }
        }

        public bool MsgProcessedRate(string phoneNumber, String from, String to, out string message, out double count)
        {
            if (_numberRecords.Count <= 0)
            {
                message = "No message to be processed";
                count = 0;
                return true;
            }

            bool validPhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber);
            if (validPhoneNumber) {
                if (!phoneNumber.All(char.IsDigit))
                {
                    message = "Invalid phone number";
                    count = 0;
                    return true;
                }
            } else {
                message = "Invalid phone number";
                count = 0;
                return true;
            }

            phoneNumber = phoneNumber.Trim();

            List<DateTime> records = _numberRecords.FirstOrDefault(pair => pair.Key == phoneNumber).Value ?? [];   

            if (records.Count <= 0) {
                count = 0;
                message = "No message matches the criteria";
                return true;
            }

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                count = 0;
                message = "Date cannot be null";
                return true;
            } else
            {
                from = from.Trim();
                to = to.Trim();
            }
                           
            if (DateTime.TryParseExact(from, FormatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDate)
                    && DateTime.TryParseExact(to, FormatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDate))
            {
                if (fromDate > toDate) {
                    count = 0;
                    message = "From Date cannot later than To Date";
                    return true;
                }

                records = FilterByDateRange(records, fromDate, toDate);

            } else 
            {
                count = 0;
                message = "Invalid date format";
                return true;
            }

            var totalMessages = records.Count;
            if (totalMessages <= 0) {
                count = 0;
                message = "No message matches the criteria";
                return true;
            }

            var seconds = (records.Max() - records.Min()).TotalSeconds; // max <= to, min >= from
            if (seconds <= 0 )
            {
                seconds = 1;
            }
            count = totalMessages / seconds;
            message = "Processed " + count.ToString() + " messages per second";
            return true;
        }

        public bool MsgProcessedAccount(String from, String to, out string message, out double count)
        {
            List<DateTime> records = [.. _numberRecords.Values.SelectMany(records => records)]; 

            var totalMessages = records.Count;
            if (totalMessages <= 0)
            {
                count = 0;
                message = "No message to be processed";
                return true;
            }

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                count = 0;
                message = "Date cannot be null";
                return true;
            } else
            {
                from = from.Trim();
                to = to.Trim();
            }
                           
            if (DateTime.TryParseExact(from, FormatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDate)
                    && DateTime.TryParseExact(to, FormatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDate))
            {
                if (fromDate > toDate) {
                    count = 0;
                    message = "From Date cannot later than To Date";
                    return true;
                }

                records = FilterByDateRange(records, fromDate, toDate);

            } else 
            {
                count = 0;
                message = "Invalid date format";
                return true;
            }

            var seconds = (records.Max() - records.Min()).TotalSeconds; // max <= to, min >= from
            if (seconds <= 0 )
            {
                seconds = 1;
            }
            count = totalMessages / seconds;
            message = "Processed " + count.ToString() + " messages per second";
            return true;
        }

        private List<DateTime> FilterByDateRange(List<DateTime> list, DateTime from, DateTime to)
        {
             return [.. list.Where(date => date >= from && date <= to)];
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
                _numberRecords.Clear();
            }

            message = "SMS limit reset.";
            return true;
        }

        public ConcurrentDictionary<string, int> GetNumberUsage() {
            return _numberUsage;
        }

        public ConcurrentDictionary<string, List<DateTime>> GetNumberRecords() {
            return _numberRecords;
        }
        public void RemoveExpiredNumbers(List<string> expiredNumbers) {
            lock (_removeNumberLock)
            {
                foreach (var number in expiredNumbers) {
                    _numberRecords.TryRemove(number, out _);
                    _numberUsage.TryRemove(number, out _);
                }
            };
        }
    }
}
