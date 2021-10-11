using PubbieSubbie;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PubbieSubbieRunner
{
    internal class SomeDriverOrWhatever : ISubscriber<DriverMessage>
    {
        private readonly ILogger<SomeDriverOrWhatever> _logger;

        public SomeDriverOrWhatever(ILogger<SomeDriverOrWhatever> logger)
        {
            _logger = logger;
        }

        public Task HandleEventAsync(DriverMessage driverEvent)
        {
            _logger.LogInformation("Driver {DriverId} goes {DriverNoise} at {Timestamp}", driverEvent.DriverId, driverEvent.Stuff, DateTime.Now.ToString("ss.fffffff"));
            return Task.CompletedTask;
        }
    }
}