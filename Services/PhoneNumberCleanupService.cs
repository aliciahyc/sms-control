using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmsControl.Services
{
    public class PhoneNumberCleanupService : BackgroundService
    {
        private readonly SmsService _smsService;
        private readonly TimeSpan _inactiveThreshold = TimeSpan.FromDays(30); // Expire numbers inactive for 30 days

        public PhoneNumberCleanupService(SmsService smsService)
            {
                _smsService = smsService;
            }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run once a day

                var currentTime = DateTime.Now;
                // Get phone numbers that haven't had activity within the last 30 days
                var expiredNumbers = _smsService.GetNumberRecords()
                                        .Where(pair => (currentTime - pair.Value.Last()) >= _inactiveThreshold)
                                        .Select(pair => pair.Key)
                                        .ToList();
                _smsService.RemoveExpiredNumbers(expiredNumbers);   
            }
        }
    }
}
