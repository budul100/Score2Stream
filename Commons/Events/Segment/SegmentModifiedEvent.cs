using Prism.Events;

namespace Score2Stream.Commons.Events.Segment
{
    public class SegmentModifiedEvent
        : PubSubEvent<Models.Contents.Segment>
    { }
}