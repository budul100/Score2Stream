using Prism.Events;
using Score2Stream.Commons.Enums;

namespace Score2Stream.Commons.Events.Menu
{
    public class TabChangedEvent
        : PubSubEvent<ViewType>
    { }
}