using EventsPubSub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EventsPubSubTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddSingleton<EventsNexus>()
                .AddTransient<ISubscriber<CheeseEvent>, CheesyHandler>()
                .AddScoped<ISubscriber<DriverEvent>, SomeDriverOrWhatever>()
                .AddLogging(loggerBuilder =>
                {
                    loggerBuilder.ClearProviders();
                    loggerBuilder.AddConsole();
                })
                .BuildServiceProvider();

            ////configure console logging
            //serviceProvider
            //    .GetService<ILoggerFactory>()
            //    .AddConsole(LogLevel.Debug);

            var task = Task.Run(() => DoStuff(serviceProvider));
            task.Wait();
        }

        private static async Task DoStuff(ServiceProvider serviceProvider)
        {
            var eventsNexus = serviceProvider.GetService<EventsNexus>();

                while (true)
                {
                    eventsNexus.PublishEvent(new CheeseEvent("Gouda"));
                    await Task.Delay(1000);
                    eventsNexus.PublishEvent(new DriverEvent(69, "beep bitshift bop"));
                    await Task.Delay(1000);
                }
        }
    }
}
