using Prism.Events;

namespace Score2Stream.Commons.Events.Clip
{
    public class SegmentSelectedEvent
        : PubSubEvent<Models.Contents.Segment>
    { }
}