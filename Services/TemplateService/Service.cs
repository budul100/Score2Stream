using Core.Events;
using Core.Interfaces;
using Core.Models;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;

namespace TemplateService
{
    public class Service
        : ITemplateService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<SelectTemplateEvent>().Subscribe(
                action: c => SelectTemplate(c),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SelectSampleEvent>().Subscribe(
                action: s => SelectSample(s),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Sample Sample { get; private set; }

        public Template Template { get; private set; }

        public List<Template> Templates { get; } = new List<Template>();

        #endregion Public Properties

        #region Public Methods

        public void AddSample()
        {
            if (Template != default)
            {
                var current = new Sample
                {
                    Image = Template.Clip.Image,
                    Content = Template.Clip.Content,
                };

                Template.Samples.Add(current);

                eventAggregator.GetEvent<SamplesChangedEvent>()
                    .Publish(Template);

                SelectSample(current);
            }
        }

        public void RemoveSample()
        {
            if (Sample != default
                && Template?.Samples?.Any() == true)
            {
                Template.Samples.Remove(Sample);

                eventAggregator.GetEvent<SamplesChangedEvent>()
                    .Publish(Template);

                SelectSample(default);
            }
        }

        public void RemoveTemplate()
        {
            if (Template != default)
            {
                if (Template.Clip.Template != default)
                {
                    Template.Clip.Template = default;
                }

                Templates.Remove(Template);

                eventAggregator
                    .GetEvent<TemplatesChangedEvent>()
                    .Publish();

                SelectTemplate(default);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void SelectSample(Sample sample)
        {
            Sample = sample;

            eventAggregator
                .GetEvent<SampleSelectedEvent>()
                .Publish(sample);
        }

        private void SelectTemplate(Clip clip)
        {
            if (clip != default)
            {
                Template = Templates
                    .SingleOrDefault(t => t.Clip == clip);

                if (Template == default)
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

                    Template = current;
                }

                clip.Template = Template;

                eventAggregator
                    .GetEvent<SelectClipEvent>()
                    .Publish(Template.Clip);
            }
            else
            {
                Template = default;
            }

            eventAggregator
                .GetEvent<TemplateSelectedEvent>()
                .Publish(Template);
        }

        #endregion Private Methods
    }
}