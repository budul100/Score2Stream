using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.Commons;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;

namespace Score2Stream.SampleService
{
    public class Service
        : ISampleService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IMessageBoxService messageBoxService;
        private readonly IRecognitionService recognitionService;
        private readonly Session settings;
        private readonly ISettingsService<Session> settingsService;

        private int index;
        private bool orderDescending;
        private Template template;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IRecognitionService recognitionService,
            IMessageBoxService messageBoxService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.recognitionService = recognitionService;
            this.messageBoxService = messageBoxService;
            this.eventAggregator = eventAggregator;

            this.settings = settingsService.Get();
        }

        #endregion Public Constructors

        #region Public Properties

        public Sample Active { get; private set; }

        public bool IsDetection { get; set; }

        public bool NoRecognition
        {
            get { return settings.Detection.NoRecognition; }
            set
            {
                if (settings.Detection.NoRecognition != value)
                {
                    settings.Detection.NoRecognition = value;
                    settingsService.Save();
                }
            }
        }

        public List<Sample> Samples { get; private set; } = new List<Sample>();

        public int ThresholdDetecting
        {
            get { return settings.Detection.ThresholdDetecting; }
            set
            {
                if (settings.Detection.ThresholdDetecting != value)
                {
                    settings.Detection.ThresholdDetecting = value;
                    settingsService.Save();
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Add(Sample sample)
        {
            if (sample?.Mat != default)
            {
                var centeredImage = sample.Mat.ToCentered(
                    fullWidth: sample.Width,
                    fullHeight: sample.Height);

                sample.Bitmap = new Bitmap(centeredImage.ToMemoryStream());

                if (sample.Value == default
                    && recognitionService != default
                    && !NoRecognition)
                {
                    sample.Value = recognitionService.Recognize(centeredImage);
                }

                Samples.Add(sample);

                orderDescending = false;
            }
        }

        public void Clear()
        {
            if (Samples.Any())
            {
                for (var index = Samples.Count; index > 0; index--)
                {
                    var sample = Samples[index - 1];
                    RemoveSample(sample);
                }

                eventAggregator.GetEvent<SamplesChangedEvent>().Publish();

                Select(default);
            }
        }

        public async Task ClearAsync()
        {
            var result = await messageBoxService.GetMessageBoxResultAsync(
                contentMessage: "Shall all samples be removed?",
                contentTitle: "Remove all samples");

            if (result == ButtonResult.Yes)
            {
                Clear();
            }
        }

        public void Create(Clip clip)
        {
            AddClip(
                clip: clip,
                select: true);
        }

        public void Initialize(Template template)
        {
            this.template = template;
        }

        public void Next(bool backward)
        {
            var next = Samples.GetNext(
                active: Active,
                backward: backward);

            if (next != default)
            {
                Select(next);
            }
        }

        public void Order()
        {
            if (orderDescending)
            {
                Samples = Samples
                    .OrderByDescending(s => string.IsNullOrWhiteSpace(s.Value))
                    .ThenByDescending(s => s.Value).ToList();
            }
            else
            {
                Samples = Samples
                    .OrderByDescending(s => string.IsNullOrWhiteSpace(s.Value))
                    .ThenBy(s => s.Value).ToList();
            }

            orderDescending = !orderDescending;

            index = 0;

            foreach (var sample in Samples)
            {
                sample.Index = index++;
            }

            eventAggregator.GetEvent<SamplesOrderedEvent>().Publish();
        }

        public async Task RemoveAsync()
        {
            if (Active != default)
            {
                var result = ButtonResult.Yes;

                if (Active.IsVerified)
                {
                    result = await messageBoxService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the selected sample be removed?",
                        contentTitle: "Remove sample");
                }

                if (result == ButtonResult.Yes)
                {
                    var next = Samples.GetNext(Active);

                    RemoveSample(Active);

                    Select(next);

                    eventAggregator.GetEvent<SamplesChangedEvent>().Publish();
                }
            }
        }

        public void Select(Sample sample)
        {
            if (Active != sample)
            {
                Active = Active != sample
                    ? sample
                    : default;

                eventAggregator.GetEvent<SampleSelectedEvent>().Publish(Active);
            }
        }

        public void Update(Clip clip)
        {
            if (clip != default)
            {
                var relevant = GetSimilar(clip);

                if (IsDetection
                    && (relevant == default || relevant.Type == SampleType.None))
                {
                    AddClip(
                        clip: clip,
                        select: false);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddClip(Clip clip, bool select)
        {
            var sample = GetSample(clip);

            if (sample != default)
            {
                Add(sample);

                eventAggregator.GetEvent<SamplesChangedEvent>().Publish();

                if (clip.Template == default)
                {
                    clip.Template = sample.Template;
                    clip.TemplateName = sample.Template.Name;

                    eventAggregator.GetEvent<TemplateSelectedEvent>().Publish(sample.Template);
                }

                if (select)
                {
                    Select(sample);
                }
            }
        }

        private Sample GetSample(Clip clip)
        {
            var result = default(Sample);

            if (clip != default)
            {
                result = new Sample
                {
                    Height = clip.Height,
                    Width = clip.Width,
                    Mat = clip.Mat,
                    Index = index++,
                    Template = template,
                };

                template.Samples.Add(result);
            }

            return result;
        }

        private Sample GetSimilar(Clip clip)
        {
            var result = default(Sample);

            if (Samples?.Any() == true)
            {
                var thresholdDetecting = Math.Abs(ThresholdDetecting) / Constants.ThresholdDivider;

                foreach (var sample in Samples)
                {
                    sample.Similarity = sample.Mat.GetSimilarityTo(clip.Mat);

                    sample.Type = sample.Similarity < thresholdDetecting
                        ? SampleType.None
                        : SampleType.Match;
                }

                result = Samples
                    .OrderByDescending(c => c.Similarity).FirstOrDefault();
            }

            return result;
        }

        private void RemoveSample(Sample sample)
        {
            if (sample != default)
            {
                sample.Template?.Samples.Remove(sample);

                Samples.Remove(sample);

                orderDescending = false;
            }
        }

        #endregion Private Methods
    }
}