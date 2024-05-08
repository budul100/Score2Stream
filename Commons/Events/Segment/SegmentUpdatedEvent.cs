using Prism.Events;

namespace Score2Stream.Commons.Events.Clip
{
    public class SegmentUpdatedEvent
        : PubSubEvent<Models.Contents.Segment>
    { }
}