using PubbieSubbie;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PubbieSubbieRunner
{
    public class CheesyHandler : ISubscriber<CheeseMessage>
    {
        private readonly ILogger<CheesyHandler> _logger;
        private readonly MessageNexus _eventsNexus;

        public CheesyHandler(ILogger<CheesyHandler> logger, MessageNexus eventsNexus)
        {
            _logger = logger;
            _eventsNexus = eventsNexus;
        }

        public async Task HandleEventAsync(CheeseMessage cheeseEvent)
        {
            _logger.LogInformation("The cheese was {CheeseType} at {Timestamp}", cheeseEvent.CheeseType, DateTime.Now.ToString("ss.fffffff"));
            if(cheeseEvent.CheeseType == "Gouda")
            {
                await Task.Delay(100);
                await _eventsNexus.PublishEventAsync(new CheeseMessage("Brie"));
            }
        }
    }
}