using Prism.Events;

namespace Score2Stream.Core.Events.Sample
{
    public class SampleUpdatedEvent
        : PubSubEvent<Models.Sample>
    { }
}