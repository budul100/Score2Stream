﻿using Core.Models.Receiver;
using Prism.Events;

namespace Core.Events
{
    public class TemplateSelectedEvent
        : PubSubEvent<Template>
    { }
}