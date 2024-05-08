using Prism.Events;

namespace Score2Stream.Commons.Events.Clip
{
    public class SegmentModifiedEvent
        : PubSubEvent<Models.Contents.Segment>
    { }
}