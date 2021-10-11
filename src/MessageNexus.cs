#nullable enable
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PubbieSubbie
{
    public abstract record BaseMessage();

    public class MessageNexus
    {
        private const string HandleEventMethodName = "HandleEventAsync";
        private const string GetIdMethodName = "UniqueIdentifier";


        private readonly ConcurrentDictionary<Type, SubscriberChannelInfo> _messageChannels;
        private readonly IServiceScopeFactory _scopeFactory;

        public MessageNexus(IServiceScopeFactory scopeFactory)
        {
            _messageChannels = new();
            _scopeFactory = scopeFactory;
        }

        public async Task PublishEventAsync<TEventType>(TEventType publishedEvent) where TEventType : BaseMessage
        {
            using var scope = _scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var (subscriberType, uniqueSubscriberType) = GetOrCreateSubscriberTypes(publishedEvent);

            foreach (var subscriber in serviceProvider.GetServices(subscriberType))
            {
                var subscriberQueue = GetOrCreateSubscriberQueue(subscriberType);
                await subscriberQueue.Channel.Writer.WriteAsync(publishedEvent);
            }

            foreach (var uniqueSubscriber in serviceProvider.GetServices(uniqueSubscriberType))
            {
                var uniqueSubscriberQueue = GetOrCreateUniqueSubscriberQueue(uniqueSubscriberType);
                await uniqueSubscriberQueue.Channel.Writer.WriteAsync(publishedEvent);
            }
        }

        /// <summary>
        /// Haal de queue-info voor dit subscriber-type op 
        /// </summary>
        /// <param name="subscriberType">Het type waarvoor we de queue info op willen halen</param>
        /// <param name="method">
        /// De method-info over hoe deze subsciber's ISubscriber.HandleEventAsync method aan-te-roepen zodat 
        /// we deze kunnen cacheén op het queue object zelf ipv deze bij elke aanroep opnieuw te moeten reflectionen</param>
        /// <returns>De queue info</returns>
        private SubscriberChannelInfo GetOrCreateSubscriberQueue(Type subscriberType)
            => _messageChannels.GetOrAdd(subscriberType, (_) => CreateSubscriberChannel(subscriberType));


        private SubscriberChannelInfo CreateSubscriberChannel(Type subscriberType)
        {
            var channelInfo = new SubscriberChannelInfo
            {
                Channel             = Channel.CreateUnbounded<BaseMessage>(),
                SubscriberInvoker   = subscriberType.GetMethod(HandleEventMethodName),
                SubscriberType      = subscriberType,
                CancellationToken   = new CancellationToken()
            };

            Task.Run(() => ReadChannelAsync(subscriberType));

            return channelInfo;
        }

        private SubscriberChannelInfo GetOrCreateUniqueSubscriberQueue(Type uniqueSubscriberType)
        {
            var getIdMethod = GetOrCreateGetIdMethod(uniqueSubscriberType);

            if (getIdMethod != null)
            {
                return _messageChannels.GetOrAdd(uniqueSubscriberType, (_) => new SubscriberChannelInfo
                {
                    Channel = Channel.CreateUnbounded<BaseMessage>(),
                    SubscriberInvoker = uniqueSubscriberType.GetMethod(HandleEventMethodName),
                    GetIdMethod = getIdMethod,
                    SubscriberType = uniqueSubscriberType
                });
            }
            else
            {
                throw new Exception($"Deze unique subscription {uniqueSubscriberType} heeft geen id getter? 😶 ");
            }
        }


        /// <summary>
        /// Heel sub-optimale manier om de queue te verwerken, niek had hier iets moois voor maar dit is een
        /// prototype dus ehh 😶
        /// Voor elke subscriber moet een Task gestart worden voor deze method.
        /// </summary>
        /// <param name="subscriberType"></param>
        private async Task ReadChannelAsync(Type subscriberType)
        {
            var channelInfo = _messageChannels[subscriberType];
            var channelReader = channelInfo.Channel.Reader;

            while (await channelReader.WaitToReadAsync())
            {
                var latestAndGreatestEvent = await channelReader.ReadAsync();
                using var scope = _scopeFactory.CreateScope();
                var serviceProvider = scope.ServiceProvider;
                var subscriber = serviceProvider.GetService(subscriberType);

                if (subscriber != null && channelInfo.SubscriberInvoker != null)
                {
                    var task = channelInfo.SubscriberInvoker.Invoke(subscriber, new object[] { latestAndGreatestEvent });
                    if (task is Task reallyATask)
                    {
                        await reallyATask;
                    }
                    else
                    {
                        throw new Exception("Geen subscriber geregistreerd, of deze heeft geen ");
                    }
                }
                else
                {
                    throw new Exception("Geen subscriber geregistreerd, of deze heeft geen ");
                }
            }
        }

        // TODO Door de type als key te gebruiken hebben we bij elke aanroep een reflection call,
        //  als we dit vervangen met iets niet-reflectie based voor de key is dat een stuk 
        //  optimaler qua performance. Je zou ook uberhaubt een lookup-tabel kunnen maken voor 
        //  het type van de event en op die manier heel veel performance winnen.
        private static (Type subscriber, Type uniqueSubscriber) GetOrCreateSubscriberTypes(BaseMessage message)
            =>  (
                    typeof(ISubscriber<>).MakeGenericType(message.GetType()),
                    typeof(IUniqueSubscriber<>).MakeGenericType(message.GetType())
                );

        // TODO Ook deze moet gecached worden
        private static MethodInfo? GetOrCreateGetIdMethod(Type subscriberType)
            => subscriberType.GetMethod(GetIdMethodName);


        private struct SubscriberChannelInfo
        {
            // public ConcurrentQueue<TEventType> Queue;
            public Type SubscriberType;
            public MethodInfo? SubscriberInvoker;
            public MethodInfo? GetIdMethod;
            public Channel<BaseMessage> Channel;
            public CancellationToken CancellationToken;
        }
    }
}
