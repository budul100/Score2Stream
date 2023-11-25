using Prism.Events;

namespace Score2Stream.Commons.Events.Sample
{
    public class SampleSelectedEvent
        : PubSubEvent<Models.Contents.Sample>
    { }
}