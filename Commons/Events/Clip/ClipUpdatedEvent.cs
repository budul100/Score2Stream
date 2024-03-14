using Prism.Events;

namespace Score2Stream.Commons.Events.Clip
{
    public class ClipUpdatedEvent
        : PubSubEvent<Models.Contents.Segment>
    { }
}