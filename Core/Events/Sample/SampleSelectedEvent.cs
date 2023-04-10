using Core.Models;
using Prism.Events;

namespace Core.Events.Sample
{
    public class SampleSelectedEvent
        : PubSubEvent<Models.Sample>
    { }
}