using Core.Models;
using Prism.Events;

namespace Core.Events.Samples
{
    public class SampleDetectedEvent
        : PubSubEvent<Clip>
    { }
}