using Prism.Events;

namespace Score2Stream.Commons.Events.Sample
{
    public class SampleModifiedEvent
        : PubSubEvent<Models.Contents.Sample>
    { }
}