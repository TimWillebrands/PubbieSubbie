using EventsPubSub;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EventsPubSubTest
{
    public class CheesyHandler : ISubscriber<CheeseEvent>
    {
        private readonly ILogger<CheesyHandler> _logger;
        private readonly EventsNexus _eventsNexus;

        public CheesyHandler(ILogger<CheesyHandler> logger, EventsNexus eventsNexus)
        {
            _logger = logger;
            _eventsNexus = eventsNexus;
        }

        public Task HandleEventAsync(CheeseEvent cheeseEvent)
        {
            _logger.LogInformation("The cheese was {CheeseType}", cheeseEvent.CheeseType);
            if(cheeseEvent.CheeseType == "Gouda")
            _eventsNexus.PublishEvent(new CheeseEvent("Brie"));

            return Task.CompletedTask;
        }
    }
}