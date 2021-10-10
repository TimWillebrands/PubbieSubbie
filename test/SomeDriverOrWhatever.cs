using EventsPubSub;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EventsPubSubTest
{
    internal class SomeDriverOrWhatever : ISubscriber<DriverEvent>
    {
        private readonly ILogger<SomeDriverOrWhatever> _logger;

        public SomeDriverOrWhatever(ILogger<SomeDriverOrWhatever> logger)
        {
            _logger = logger;
        }

        public Task HandleEventAsync(DriverEvent driverEvent)
        {
            _logger.LogInformation("Driver {DriverId} goes {DriverNoise}", driverEvent.DriverId, driverEvent.Stuff);
            return Task.CompletedTask;
        }
    }
}