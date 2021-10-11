namespace PubbieSubbie
{
    public interface IUniqueSubscriber<T> : ISubscriber<T>  where T : BaseMessage
    {
        public string UniqueIdentifier { get; }
    }
}
