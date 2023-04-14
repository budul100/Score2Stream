using Prism.Events;

namespace Score2Stream.Core.Events.Scoreboard
{
    public class ScoreboardUpdatedEvent
        : PubSubEvent<string>
    { }
}