using Prism.Events;

namespace Core.Events
{
    public class ScoreboardUpdatedEvent
        : PubSubEvent<string>
    { }
}