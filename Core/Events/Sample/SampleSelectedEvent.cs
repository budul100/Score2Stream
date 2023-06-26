using Prism.Events;

namespace Score2Stream.Core.Events.Sample
{
    public class SampleSelectedEvent
        : PubSubEvent<Models.Contents.Sample>
    { }
}