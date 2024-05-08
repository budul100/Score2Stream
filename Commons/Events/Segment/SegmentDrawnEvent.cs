using Prism.Events;

namespace Score2Stream.Commons.Events.Clip
{
    public class SegmentDrawnEvent
        : PubSubEvent<Models.Contents.Segment>
    { }
}