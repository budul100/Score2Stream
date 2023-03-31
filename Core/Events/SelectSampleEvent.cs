using Prism.Events;
using ScoreboardOCR.Core.Models;

namespace Core.Events
{
    public class SelectSampleEvent
        : PubSubEvent<Sample>
    { }
}