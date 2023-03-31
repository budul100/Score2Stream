using Prism.Events;
using ScoreboardOCR.Core.Models;

namespace Core.Events
{
    public class TemplateSelectedEvent
        : PubSubEvent<Template>
    { }
}