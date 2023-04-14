using Prism.Events;

namespace Score2Stream.Core.Events.Sample
{
    public class SampleDetectedEvent
        : PubSubEvent<Models.Clip>
    { }
}