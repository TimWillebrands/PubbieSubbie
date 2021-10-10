using EventsPubSub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventsPubSubTest
{
    public record CheeseEvent(string CheeseType) : BaseEvent();
    public record DriverEvent(int DriverId, string Stuff) : BaseEvent();
}
