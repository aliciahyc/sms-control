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
                    { "SmsControl:MaxMessagesPerNumberPerSecond", "2" },
                    { "SmsControl:MaxMessagesPerAccountPerSecond", "5" }
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
            Thread.Sleep(30000);
            
            // Get Process rate for all phone numbers and all time
            bool result = _smsService.MsgProcessedRate("", "", "", out _, out double count1);
            Assert.True(result);
            Assert.True(count1 > 0);

            // Get Process rate for a phone numbers and all time
            result = _smsService.MsgProcessedRate("12345", "12", "34", out _, out double count2); // invalid from and to Dates
            Assert.True(result);
            Assert.True(count2 > 0);

            // Get Process rate for a phone numbers and all time
            var to = DateTime.Now;
            result = _smsService.MsgProcessedRate("12345", from.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    to.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), out _, out double count3);
            Assert.True(result);
            Assert.True(count3 > 0);
        }

        public void Dispose() 
        {
            _smsService.ResetLimit(out _);
        }
    }
}
