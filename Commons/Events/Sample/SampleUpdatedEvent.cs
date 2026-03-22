using Prism.Events;

namespace Score2Stream.Commons.Events.Sample
{
    public class SampleUpdatedEvent
        : PubSubEvent<Models.Contents.Sample>
    { }
}