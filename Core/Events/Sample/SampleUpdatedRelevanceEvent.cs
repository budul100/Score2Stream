using Prism.Events;

namespace Score2Stream.Core.Events.Sample
{
    public class SampleUpdatedRelevanceEvent
        : PubSubEvent<Models.Contents.Sample>
    { }
}