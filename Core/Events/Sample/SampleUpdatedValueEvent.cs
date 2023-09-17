using Prism.Events;

namespace Score2Stream.Core.Events.Sample
{
    public class SampleUpdatedValueEvent
        : PubSubEvent<Models.Contents.Sample>
    { }
}