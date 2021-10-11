using PubbieSubbie;

namespace PubbieSubbieRunner
{
    public record CheeseMessage(string CheeseType) : BaseMessage();
    public record DriverMessage(int DriverId, string Stuff) : BaseMessage();
}
