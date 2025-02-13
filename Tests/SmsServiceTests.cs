using System.Globalization;
using SmsControl.Services;
//using Microsoft.Extensions.Configuration;
using Xunit;

namespace SmsControl.Tests 
{
    public class SmsServiceTests: IDisposable 
    {
        private readonly SmsService _smsService;

        public SmsServiceTests() 
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "SmsControl:MaxMessagesPerNumber", "2" },
                    { "SmsControl:MaxMessagesPerAccount", "5" }
                })
                .Build();

            _smsService = new SmsService(config);
        }

        [Fact]
        public void AllowsMessage_WhenUnderLimit() 
        {
            bool result = _smsService.CanSendMessage("12345", out string message);
            Assert.True(result);
            Assert.Equal("SMS message allowed", message);
        }

        [Fact]
        public void ExceedingLimitPerPhone() 
        {
            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("12345", out _);
            bool result = _smsService.CanSendMessage("12345", out string message);
            Assert.False(result);
            Assert.Equal("Limit exceeded for this phone number", message);
            _smsService.CanSendMessage("67890", out _);
            _smsService.CanSendMessage("67890", out _);
        }

        [Fact]
        public void ExceedingLimitPerAccount() 
        {
            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("67890", out _);
            bool result = _smsService.CanSendMessage("67890", out string message);
            Assert.False(result);
            Assert.Equal("Limit exceeded for the entire account", message);
        }

        [Fact]
        public void InvalidPhoneNumber() 
        {
            bool result = _smsService.CanSendMessage("", out string message1);
            Assert.False(result);
            Assert.Equal("Invalid phone number", message1);
            result = _smsService.CanSendMessage("  ", out string message2);
            Assert.False(result);
            Assert.Equal("Invalid phone number", message2);
            result = _smsService.CanSendMessage("123abc", out string message3);
            Assert.False(result);
            Assert.Equal("Invalid phone number", message3);
        }

        [Fact]
        public void RemoveExpiredNumbers() 
        {
            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("67890", out _);
            List<string> expiredNumbers = ["23456"];
            _smsService.RemoveExpiredNumbers(expiredNumbers);
            // Able to send message after remove expired number
            bool result = _smsService.CanSendMessage("23456", out string message);
            Assert.True(result);
            Assert.Equal("SMS message allowed", message);
        }

        [Fact]
        public void MsgProcessedRate() 
        {
            var from = DateTime.Now;

            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("67890", out _);
            Thread.Sleep(10000);

            var to = DateTime.Now;

            bool result = _smsService.MsgProcessedRate("12345", from.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    to.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), out _, out double count1);
            Assert.True(result);
            Assert.True(count1 > 0);

            // Invalid from and to Dates, return 0 message
            result = _smsService.MsgProcessedRate("12345", "", "", out _, out double count2);
            Assert.True(result);
            Assert.True(count2 == 0);

            // Invalid phone number, return 0 message
            result = _smsService.MsgProcessedRate("1ab", from.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    to.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), out _, out double count3);
            Assert.True(result);
            Assert.True(count3 == 0);

            // Invalid phone number, return 0 message
            result = _smsService.MsgProcessedRate("11111", from.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    to.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), out _, out double count4);
            Assert.True(result);
            Assert.True(count4 == 0);

            // Invalid from and to Dates, return 0 message
            result = _smsService.MsgProcessedRate("12345", "12", "34", out _, out double count5); 
            Assert.True(result);
            Assert.True(count5 == 0);

            result = _smsService.MsgProcessedRate("12345", "2025-02-10", 
                    "2025-02-13", out _, out double count6);
            Assert.True(result);
            Assert.True(count6 > 0);
        }

        [Fact]
        public void MsgProcessedAccount() 
        {
            var from = DateTime.Now;

            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("12345", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("23456", out _);
            _smsService.CanSendMessage("67890", out _);
            Thread.Sleep(10000);

            var to = DateTime.Now;
            
            // Get Process rate by account for a time span
            bool result = _smsService.MsgProcessedAccount(from.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    to.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), out _, out double count1);
            Assert.True(result);
            Assert.True(count1 > 0);

            // Invalid time span, return 0 message
            result = _smsService.MsgProcessedAccount("", "", out _, out double count2);
            Assert.True(result);
            Assert.True(count2 == 0);

            // invalid from and to Dates, return 0 message
            result = _smsService.MsgProcessedAccount("12", "34", out _, out double count3); 
            Assert.True(result);
            Assert.True(count3 == 0);

            result = _smsService.MsgProcessedAccount("2025-02-10", 
                    "2025-02-13", out _, out double count4);
            Assert.True(result);
            Assert.True(count4 > 0);
        }

        public void Dispose() 
        {
            _smsService.ResetLimit(out _);
        }
    }
}
