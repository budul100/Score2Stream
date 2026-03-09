using Prism.Events;

namespace Score2Stream.Commons.Events.Input
{
    public class InputEndedEvent
        : PubSubEvent<Models.Contents.Input>
    { }
}