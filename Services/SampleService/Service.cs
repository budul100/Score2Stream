using Core.Events.Samples;
using Core.Interfaces;
using Core.Models;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;

namespace SampleService
{
    public class Service
        : ISampleService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public Sample Sample { get; private set; }

        public List<Sample> Samples { get; } = new List<Sample>();

        #endregion Public Properties

        #region Public Methods

        public void Add(Clip clip)
        {
            if (clip != default)
            {
                var sample = new Sample
                {
                    Image = clip.Image,
                    Bitmap = clip.Bitmap,
                    Template = clip.Template,
                };

                clip.Template.Samples.Add(sample);

                Samples.Add(sample);

                eventAggregator
                    .GetEvent<SamplesChangedEvent>()
                    .Publish();

                Select(sample);
            }
        }

        public void Remove(Template template)
        {
            if (template?.Samples?.Any() == true)
            {
                var samples = template.Samples.ToArray();

                for (var index = 0; index < samples.Length; index++)
                {
                    RemoveSample(samples[index]);
                }

                eventAggregator.GetEvent<SamplesChangedEvent>()
                    .Publish();

                Select(default);
            }
        }

        public void Remove()
        {
            RemoveSample(Sample);

            eventAggregator.GetEvent<SamplesChangedEvent>()
                .Publish();

            Select(default);
        }

        public void Select(Sample sample)
        {
            Sample = sample;

            eventAggregator
                .GetEvent<SampleSelectedEvent>()
                .Publish(sample);
        }

        #endregion Public Methods

        #region Private Methods

        private void RemoveSample(Sample sample)
        {
            if (sample != default)
            {
                sample.Template.Samples.Remove(sample);

                Samples.Remove(sample);
            }
        }

        #endregion Private Methods
    }
}