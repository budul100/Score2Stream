using Core.Models.Receiver;
using Prism.Events;

namespace Core.Events
{
    public class SelectClipEvent
        : PubSubEvent<Clip>
    { }
}