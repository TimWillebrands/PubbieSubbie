using System.Threading.Tasks;

namespace PubbieSubbie
{
    public interface ISubscriber<T> where T : BaseMessage
    {
        public Task HandleEventAsync(T @event);
    }
}
