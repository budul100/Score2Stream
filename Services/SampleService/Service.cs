using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Score2Stream.SampleService
{
    public class Service
        : ISampleService
    {
        #region Private Fields

        private readonly IDialogService dialogService;
        private readonly IRecognitionService recognitionService;
        private readonly SamplesChangedEvent samplesChangedEvent;
        private readonly SampleSelectedEvent sampleSelectedEvent;
        private readonly SamplesOrderedEvent samplesOrderedEvent;
        private readonly SampleUpdatedEvent sampleUpdatedEvent;
        private readonly ISettingsService<Session> settingsService;
        private readonly TemplateSelectedEvent templateSelectedEvent;

        private int index;
        private bool orderDescending;
        private Template template;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IRecognitionService recognitionService,
            IDialogService dialogService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.recognitionService = recognitionService;
            this.dialogService = dialogService;

            sampleUpdatedEvent = eventAggregator.GetEvent<SampleUpdatedEvent>();

            samplesChangedEvent = eventAggregator.GetEvent<SamplesChangedEvent>();
            samplesOrderedEvent = eventAggregator.GetEvent<SamplesOrderedEvent>();
            sampleSelectedEvent = eventAggregator.GetEvent<SampleSelectedEvent>();

            templateSelectedEvent = eventAggregator.GetEvent<TemplateSelectedEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsDetection { get; set; }

        public Sample Sample { get; private set; }

        public List<Sample> Samples { get; private set; } = new List<Sample>();

        #endregion Public Properties

        #region Public Methods

        public void Add(Sample sample)
        {
            if (sample?.Mat != default)
            {
                if (Samples.Count >= Constants.MaxCountSamples)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Sample),
                        maxCount: Constants.MaxCountSamples);
                }

                var centeredImage = sample.Mat.ToCentered(
                    fullWidth: sample.Width,
                    fullHeight: sample.Height);

                sample.Bitmap = new Bitmap(centeredImage.ToMemoryStream());

                if (sample.Value == default
                    && recognitionService != default
                    && !settingsService.Contents.Detection.NoRecognition)
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

                samplesChangedEvent.Publish();

                Select(default);
            }
        }

        public async Task ClearAsync()
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: "Shall all samples be removed?",
                contentTitle: "Remove all samples");

            if (result == ButtonResult.Yes)
            {
                Clear();
            }
        }

        public void Create(Segment clip)
        {
            try
            {
                AddSample(
                    clip: clip,
                    select: true);
            }
            catch (MaxCountExceededException exception)
            {
                dialogService.ShowMessageBoxAsync(
                    contentMessage: exception.Message,
                    contentTitle: "Maximum count exceeded",
                    icon: Icon.Error);
            }
        }

        public void Initialize(Template template)
        {
            this.template = template;
        }

        public void Next(bool backward)
        {
            var next = Samples.GetNext(
                active: Sample,
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

            samplesOrderedEvent.Publish();
        }

        public async Task RemoveAsync()
        {
            if (Sample != default)
            {
                var result = ButtonResult.Yes;

                if (Sample.IsVerified)
                {
                    result = await dialogService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the selected sample be removed?",
                        contentTitle: "Remove sample");
                }

                if (result == ButtonResult.Yes)
                {
                    var next = Samples.GetNext(Sample);

                    RemoveSample(Sample);

                    Select(next);

                    samplesChangedEvent.Publish();
                }
            }
        }

        public void Select(Sample sample)
        {
            if (Sample != sample)
            {
                Sample = Sample != sample
                    ? sample
                    : default;

                sampleSelectedEvent.Publish(Sample);
            }
        }

        public void Update(Segment clip)
        {
            if (clip != default)
            {
                SetSimilarities(clip);

                if (IsDetection
                    && Samples.Count < Constants.MaxCountAreas)
                {
                    var relevant = Samples
                        .OrderByDescending(c => c.Similarity).FirstOrDefault();

                    if (relevant == default || relevant.Type == SampleType.None)
                    {
                        try
                        {
                            AddSample(
                                clip: clip,
                                select: false);
                        }
                        catch (MaxCountExceededException)
                        { }
                    }
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddSample(Segment clip, bool select)
        {
            var sample = GetSample(clip);

            if (sample != default)
            {
                Add(sample);

                samplesChangedEvent.Publish();

                if (clip.Area.Template == default)
                {
                    clip.Area.Template = sample.Template;
                    clip.Area.TemplateName = sample.Template.Name;

                    templateSelectedEvent.Publish(sample.Template);
                }

                if (select)
                {
                    Select(sample);
                }
            }
        }

        private Sample GetSample(Segment clip)
        {
            var result = default(Sample);

            if (clip != default)
            {
                result = new Sample
                {
                    Height = clip.Bitmap.Size.Height,
                    Width = clip.Bitmap.Size.Width,
                    Mat = clip.Mat,
                    Index = index++,
                    Template = template,
                };

                template.Samples.Add(result);
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

        private void SetSimilarities(Segment clip)
        {
            if (Samples?.Any() == true)
            {
                var thresholdDetecting = Math.Abs(settingsService.Contents.Detection.ThresholdDetecting) / Constants.ThresholdDivider;

                foreach (var sample in Samples)
                {
                    var similarity = sample.Mat.GetSimilarityTo(
                        template: clip.Mat,
                        preventMultipleComparison: settingsService.Contents.Detection.NoMultiComparison);

                    var type = similarity < thresholdDetecting
                        ? SampleType.None
                        : SampleType.Match;

                    if (similarity != sample.Similarity
                        || type != sample.Type)
                    {
                        sample.Similarity = similarity;
                        sample.Type = type;

                        sampleUpdatedEvent.Publish(sample);
                    }
                }
            }
        }

        #endregion Private Methods
    }
}