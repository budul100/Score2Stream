using Core.Models;
using Prism.Events;

namespace Core.Events.Clip
{
    public class ClipUpdatedEvent
        : PubSubEvent<Models.Clip>
    { }
}