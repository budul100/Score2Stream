using Core.Events;
using Prism.Events;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateService
{
    public class Service
        : ITemplateService
    {
        #region Private Fields

        private readonly IClipService clipService;
        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public Service(IClipService clipService, IEventAggregator eventAggregator)
        {
            this.clipService = clipService;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<SelectTemplateEvent>().Subscribe(
                action: c => SelectTemplate(c));
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler OnSamplesChangedEvent;

        #endregion Public Events

        #region Public Properties

        public Template Selection { get; private set; }

        public List<Template> Templates { get; } = new List<Template>();

        #endregion Public Properties

        #region Public Methods

        public void AddSample()
        {
            if (Selection != default)
            {
                var current = new Sample
                {
                    Image = Selection.Clip.Image,
                    Content = Selection.Clip.Content,
                };

                Selection.Samples.Add(current);

                OnSamplesChangedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        public void RemoveSample()
        {
            throw new NotImplementedException();
        }

        public void RemoveTemplate()
        {
            if (Selection != default)
            {
                var current = Selection;

                Selection = default;

                eventAggregator
                    .GetEvent<TemplateSelectedEvent>()
                    .Publish(current);

                Templates.Remove(current);

                eventAggregator
                    .GetEvent<TemplatesChangedEvent>()
                    .Publish();
            }
        }

        public void SelectSample(Sample sample)
        {
            throw new NotImplementedException();
        }

        #endregion Public Methods

        #region Private Methods

        private void SelectTemplate(Clip clip)
        {
            Selection = Templates
                .SingleOrDefault(t => t.Clip == clip);

            if (Selection == default)
            {
                var current = new Template
                {
                    Clip = clip,
                };

                Templates.Add(
                    item: current);

                eventAggregator
                    .GetEvent<TemplatesChangedEvent>()
                    .Publish();

                Selection = current;
            }

            eventAggregator
                .GetEvent<SelectClipEvent>()
                .Publish(Selection.Clip);

            eventAggregator
                .GetEvent<TemplateSelectedEvent>()
                .Publish(Selection);
        }

        #endregion Private Methods
    }
}