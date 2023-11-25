using Prism.Events;

namespace Score2Stream.Commons.Events.Scoreboard
{
    public class ScoreboardUpdatedEvent
        : PubSubEvent<string>
    { }
}