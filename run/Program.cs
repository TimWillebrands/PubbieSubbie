using PubbieSubbie;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PubbieSubbieRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddSingleton<MessageNexus>()
                .AddTransient<ISubscriber<CheeseMessage>, CheesyHandler>()
                .AddScoped<ISubscriber<DriverMessage>, SomeDriverOrWhatever>()
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
            var eventsNexus = serviceProvider.GetService<MessageNexus>();

                while (true)
                {
                    await eventsNexus.PublishEventAsync(new CheeseMessage("Gouda"));
                    await Task.Delay(1000);
                    await eventsNexus.PublishEventAsync(new DriverMessage(69, "beep bitshift bop"));
                    await Task.Delay(1000);
                }
        }
    }
}
