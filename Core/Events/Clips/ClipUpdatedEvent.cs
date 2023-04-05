using Core.Models;
using Prism.Events;

namespace Core.Events.Clips
{
    public class ClipUpdatedEvent
        : PubSubEvent<Clip>
    { }
}