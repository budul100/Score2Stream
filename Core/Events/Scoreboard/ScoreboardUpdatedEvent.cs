using Prism.Events;

namespace Core.Events.Scoreboard
{
    public class ScoreboardUpdatedEvent
        : PubSubEvent<string>
    { }
}