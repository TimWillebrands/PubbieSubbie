#nullable enable
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace EventsPubSub
{
    public abstract record BaseEvent();

    public class EventsNexus
    {
        private const string HandleEventMethodName = "HandleEventAsync";
        private const string GetIdMethodName = "UniqueIdentifier";

        private readonly ConcurrentDictionary<Type, SubscriberQueueInfo> _subscriberEventQueues;
        private readonly IServiceScopeFactory _scopeFactory;

        public EventsNexus(IServiceScopeFactory scopeFactory)
        {
            _subscriberEventQueues = new();
            _scopeFactory = scopeFactory;
        }

        public void PublishEvent(BaseEvent publishedEvent)
        {
            using var scope     = _scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var (subscriberType, uniqueSubscriberType) = GetOrCreateSubscriberTypeInfo(publishedEvent);

            foreach (var subscriber in serviceProvider.GetServices(subscriberType))
            {
                var subscriberQueue = GetOrCreateSubscriberQueue(subscriberType);
                subscriberQueue.Queue.Enqueue(publishedEvent);
            }

            foreach (var uniqueSubscriber in serviceProvider.GetServices(uniqueSubscriberType))
            {
                var uniqueSubscriberQueue = GetOrCreateUniqueSubscriberQueue(uniqueSubscriberType);
                uniqueSubscriberQueue.Queue.Enqueue(publishedEvent);
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
        private SubscriberQueueInfo GetOrCreateSubscriberQueue(Type subscriberType)
            => _subscriberEventQueues.GetOrAdd(subscriberType, (_) => new SubscriberQueueInfo
            {
                Queue          = new ConcurrentQueue<BaseEvent>(),
                Task           = Task.Run(() => EmptyQueue(subscriberType)),
                Method         = subscriberType.GetMethod(HandleEventMethodName),
                SubscriberType = subscriberType
            });

        private SubscriberQueueInfo GetOrCreateUniqueSubscriberQueue(Type uniqueSubscriberType)
        {
            var getIdMethod = GetOrCreateGetIdMethod(uniqueSubscriberType);

            if(getIdMethod != null)
            {
                return _subscriberEventQueues.GetOrAdd(uniqueSubscriberType, (_) => new SubscriberQueueInfo
                {
                    Queue = new ConcurrentQueue<BaseEvent>(),
                    Task = Task.Run(() => EmptyQueue(uniqueSubscriberType)),
                    Method = uniqueSubscriberType.GetMethod(HandleEventMethodName),
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
        /// <returns></returns>
        private async Task EmptyQueue(Type subscriberType)
        {
            var subscriberQueue = _subscriberEventQueues[subscriberType];

            while (true)
            {
                if (subscriberQueue.Queue.TryDequeue(out var latestAndGreatestEvent))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var serviceProvider = scope.ServiceProvider;
                    var subscriber = serviceProvider.GetService(subscriberType);

                    if (subscriber != null && subscriberQueue.Method != null)
                    {
                        var task = subscriberQueue.Method.Invoke(subscriber, new object[] { latestAndGreatestEvent });
                        if(task != null)
                        {
                            await (Task)task;
                        }
                    }
                    else
                    {
                        throw new Exception("Geen subscriber geregistreerd, of deze heeft geen ");
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }

        // TODO Door de type als key te gebruiken hebben we bij elke aanroep een reflection call,
        //  als we dit vervangen met iets niet-reflectie based voor de key is dat een stuk 
        //  optimaler qua performance. Je zou ook uberhaubt een lookup-tabel kunnen maken voor 
        //  het type van de event en op die manier heel veel performance winnen.
        private static (Type subscriber, Type uniqueSubscriber) GetOrCreateSubscriberTypeInfo(BaseEvent @event)
            => (typeof(ISubscriber<>).MakeGenericType(@event.GetType()), typeof(IUniqueSubscriber<>).MakeGenericType(@event.GetType()));

        // TODO Ook deze moet gecached worden
        private static MethodInfo? GetOrCreateGetIdMethod(Type subscriberType)
            => subscriberType.GetMethod(GetIdMethodName);
        

        private struct SubscriberQueueInfo
        {
            public ConcurrentQueue<BaseEvent> Queue;
            public Type SubscriberType;
            public MethodInfo? Method;
            public MethodInfo? GetIdMethod;
            public Task Task;
        }
    }
}
