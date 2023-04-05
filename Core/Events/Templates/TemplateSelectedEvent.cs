using Core.Models;
using Prism.Events;

namespace Core.Events.Templates
{
    public class TemplateSelectedEvent
        : PubSubEvent<Template>
    { }
}