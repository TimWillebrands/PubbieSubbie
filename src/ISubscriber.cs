using System.Threading.Tasks;

namespace EventsPubSub
{
    public interface ISubscriber<T> where T : BaseEvent
    {
        public Task HandleEventAsync(T @event);
    }
}
