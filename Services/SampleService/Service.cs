using Prism.Events;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.SampleService
{
    public class Service
        : ISampleService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private int index;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<SampleDetectedEvent>().Subscribe(
                action: c => OnSampleDetected(c),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsDetection { get; set; }

        public Sample Sample { get; private set; }

        public List<Sample> Samples { get; private set; } = new List<Sample>();

        #endregion Public Properties

        #region Public Methods

        public void Add(Clip clip)
        {
            if (clip != default)
            {
                var sample = GetSample(clip);

                Select(sample);
            }
        }

        public void Next(bool onward)
        {
            var next = GetNext(onward);

            if (next != default)
            {
                Select(next);
            }
        }

        public void Order()
        {
            Samples = Samples
                .OrderByDescending(s => string.IsNullOrWhiteSpace(s.Value))
                .ThenBy(s => s.Value).ToList();

            index = 0;

            foreach (var sample in Samples)
            {
                sample.Index = index++;
            }

            eventAggregator.GetEvent<SamplesOrderedEvent>()
                .Publish();
        }

        public void Remove(Template template)
        {
            if (template?.Samples?.Any() == true)
            {
                var samples = template.Samples.ToArray();

                for (var index = template.Samples.Count; index > 0; index--)
                {
                    var sample = samples[index - 1];
                    RemoveSample(sample);
                }

                eventAggregator.GetEvent<SamplesChangedEvent>()
                    .Publish();

                Select(default);
            }
        }

        public void Remove()
        {
            var next = GetNext(true);

            RemoveSample(Sample);

            Select(next);

            eventAggregator.GetEvent<SamplesChangedEvent>()
                .Publish();
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

        private Sample GetNext(bool onward)
        {
            var result = Sample;

            if (Samples.Count > 0)
            {
                var index = Samples.IndexOf(Sample);

                if (onward)
                {
                    result = index < Samples.Count - 1
                        ? Samples[index + 1]
                        : Samples[0];
                }
                else
                {
                    result = index > 0
                        ? Samples[index - 1]
                        : Samples[^1];
                }
            }

            return result;
        }

        private Sample GetSample(Clip clip)
        {
            var result = default(Sample);

            if (clip?.Template?.Samples != default)
            {
                result = new Sample
                {
                    Bitmap = clip.Bitmap,
                    Image = clip.Image,
                    Index = index++,
                    Template = clip.Template,
                };

                clip.Template.Samples.Add(result);

                Samples.Add(result);

                eventAggregator
                    .GetEvent<SamplesChangedEvent>()
                    .Publish();
            }

            return result;
        }

        private void OnSampleDetected(Clip clip)
        {
            if (IsDetection)
            {
                GetSample(clip);
            }
        }

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