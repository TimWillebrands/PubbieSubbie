namespace EventsPubSub
{
    public interface IUniqueSubscriber<T> : ISubscriber<T>  where T : BaseEvent
    {
        public string UniqueIdentifier { get; }
    }
}
