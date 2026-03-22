using Prism.Events;

namespace Score2Stream.Commons.Events.Segment
{
    public class SegmentUpdatedEvent
        : PubSubEvent<Models.Contents.Segment>
    { }
}