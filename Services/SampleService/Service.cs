using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Menu;
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

            samplesChangedEvent = eventAggregator.GetEvent<SamplesChangedEvent>();
            samplesOrderedEvent = eventAggregator.GetEvent<SamplesOrderedEvent>();
            sampleSelectedEvent = eventAggregator.GetEvent<SampleSelectedEvent>();

            templateSelectedEvent = eventAggregator.GetEvent<TemplateSelectedEvent>();

            eventAggregator.GetEvent<FilterChangedEvent>().Subscribe(
                action: () => Order(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SegmentUpdatedEvent>().Subscribe(
                action: s => DetectSegment(s),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: _ => IsDetection);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsDetection { get; set; }

        public Sample Sample { get; private set; }

        public List<Sample> Samples { get; } = new List<Sample>();

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

                sample.Index = index++;

                Samples.Add(sample);
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

        public void Create(Segment segment)
        {
            try
            {
                AddSample(
                    segment: segment,
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
            var next = Samples
                .OrderBy(s => s.Index)
                .GetUnfiltereds()
                .GetNext(
                    active: Sample,
                    backward: backward);

            if (next != default)
            {
                Select(next);
            }
        }

        public void Order(bool reverseOrder = false)
        {
            var samples = default(IEnumerable<Sample>);

            if (orderDescending)
            {
                samples = Samples
                    .OrderByDescending(s => s.IsVerified)
                    .ThenBy(s => s.GetIndex())
                    .ThenByDescending(s => s.GetValue()).ToArray();
            }
            else
            {
                samples = Samples
                    .OrderByDescending(s => s.IsVerified)
                    .ThenBy(s => s.GetIndex())
                    .ThenBy(s => s.GetValue()).ToArray();
            }

            if (reverseOrder)
            {
                orderDescending = !orderDescending;
            }

            index = 0;

            foreach (var sample in samples)
            {
                sample.Index = index++;

                sample.IsFiltered = settingsService.Contents.Detection.FilterVerifieds
                    && sample.IsVerified;
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

        #endregion Public Methods

        #region Private Methods

        private void AddSample(Segment segment, bool select)
        {
            var sample = GetSample(segment);

            if (sample != default)
            {
                var unverifieds = Samples
                    .Where(s => !s.IsVerified).ToArray();

                if (unverifieds.Length >= settingsService.Contents.Detection.MaxCountUnverifieds)
                {
                    var relevant = unverifieds
                        .Where(s => s != Sample)
                        .OrderBy(s => s.Index).FirstOrDefault();

                    RemoveSample(relevant);
                }

                Add(sample);

                samplesChangedEvent.Publish();

                if (segment.Area.Template == default)
                {
                    segment.Area.Template = sample.Template;
                    segment.Area.TemplateName = sample.Template.Name;

                    templateSelectedEvent.Publish(sample.Template);
                }

                if (select)
                {
                    Select(sample);
                }
            }
        }

        private void DetectSegment(Segment segment)
        {
            if (segment != default)
            {
                var thresholdDetecting = Math.Abs(settingsService.Contents.Detection.ThresholdDetecting) / Constants.ThresholdDivider;

                if (segment.Matches?.Any() != true
                    || (segment.Matches.Any(m => m.Similarity > Constants.SimilarityMin)
                    && segment.Matches.All(m => m.Similarity < thresholdDetecting)))
                {
                    try
                    {
                        AddSample(
                            segment: segment,
                            select: false);
                    }
                    catch (MaxCountExceededException)
                    { }
                }
            }
        }

        private Sample GetSample(Segment segment)
        {
            var result = default(Sample);

            if (segment?.Mat != default)
            {
                result = new Sample
                {
                    Height = segment.Bitmap?.Size.Height ?? 0,
                    Width = segment.Bitmap?.Size.Width ?? 0,
                    Mat = segment.Mat,
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
            }
        }

        #endregion Private Methods
    }
}