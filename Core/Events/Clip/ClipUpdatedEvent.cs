using Prism.Events;

namespace Score2Stream.Core.Events.Clip
{
    public class ClipUpdatedEvent
        : PubSubEvent<Models.Contents.Clip>
    { }
}