using Core.Models;
using Prism.Events;

namespace Core.Events.Sample
{
    public class SampleDetectedEvent
        : PubSubEvent<Models.Clip>
    { }
}