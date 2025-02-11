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

        public void Dispose() 
        {
            _smsService.ResetLimit(out _);
        }
    }
}
