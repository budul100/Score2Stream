﻿using Prism.Events;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.TemplateService
{
    public class Service
        : ITemplateService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISampleService sampleService, IEventAggregator eventAggregator)
        {
            SampleService = sampleService;
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public ISampleService SampleService { get; }

        public Template Template { get; private set; }

        public List<Template> Templates { get; } = new List<Template>();

        #endregion Public Properties

        #region Public Methods

        public void Add(Clip clip)
        {
            var template = Templates.SingleOrDefault(t => t.Clip == clip);

            if (template == default)
            {
                template = new Template
                {
                    Clip = clip,
                };

                clip.Template = template;

                Templates.Add(template);

                eventAggregator
                    .GetEvent<TemplatesChangedEvent>()
                    .Publish();
            }

            Select(template);
        }

        public void Clear()
        {
            if (Templates.Any())
            {
                foreach (var template in Templates)
                {
                    RemoveTemplate(template);
                }

                eventAggregator.GetEvent<TemplatesChangedEvent>()
                    .Publish();

                Select(default);
            }
        }

        public void Remove()
        {
            Remove(Template);
        }

        public void Remove(Template template)
        {
            if (template != default)
            {
                RemoveTemplate(template);

                eventAggregator.GetEvent<TemplatesChangedEvent>()
                    .Publish();

                Select(default);
            }
        }

        public void Select(Template template)
        {
            Template = template;

            eventAggregator
                .GetEvent<TemplateSelectedEvent>()
                .Publish(template);
        }

        #endregion Public Methods

        #region Private Methods

        private void RemoveTemplate(Template template)
        {
            if (template != default)
            {
                SampleService.Remove(template);

                template.Clip.Template = default;

                Templates.Remove(template);
            }
        }

        #endregion Private Methods
    }
}