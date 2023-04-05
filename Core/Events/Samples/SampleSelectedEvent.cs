using Core.Models;
using Prism.Events;

namespace Core.Events.Samples
{
    public class SampleSelectedEvent
        : PubSubEvent<Sample>
    { }
}