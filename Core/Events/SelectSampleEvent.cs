﻿using Core.Models;
using Prism.Events;

namespace Core.Events
{
    public class SelectSampleEvent
        : PubSubEvent<Sample>
    { }
}