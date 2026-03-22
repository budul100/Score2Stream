using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Segment;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
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

        private readonly IDialogService dialogService;
        private readonly IRecognitionService recognitionService;
        private readonly SamplesChangedEvent samplesChangedEvent;
        private readonly SampleSelectedEvent sampleSelectedEvent;
        private readonly SamplesOrderedEvent samplesOrderedEvent;
        private readonly ISettingsService<Session> settingsService;
        private readonly TemplateSelectedEvent templateSelectedEvent;

        private int index;
        private bool orderDescending;
        private ImmutableList<Sample> samples = [];
        private Template template;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDialogService dialogService,
            IRecognitionService recognitionService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            this.recognitionService = recognitionService;

            samplesChangedEvent = eventAggregator.GetEvent<SamplesChangedEvent>();
            samplesOrderedEvent = eventAggregator.GetEvent<SamplesOrderedEvent>();
            sampleSelectedEvent = eventAggregator.GetEvent<SampleSelectedEvent>();

            templateSelectedEvent = eventAggregator.GetEvent<TemplateSelectedEvent>();

            eventAggregator.GetEvent<FilterChangedEvent>().Subscribe(
                action: () => Order(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SegmentUpdatedEvent>().Subscribe(
                action: DetectSample,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: _ => IsDetection);

            eventAggregator.GetEvent<SampleModifiedEvent>().Subscribe(
                action: _ => SaveSamples(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Sample Active { get; private set; }

        public bool IsDetection { get; set; }

        public IReadOnlyList<Sample> Samples => samples;

        #endregion Public Properties

        #region Public Methods

        public void Add(Sample sample)
        {
            if (sample?.Image != default)
            {
                var unverifieds = Samples
                    .Where(s => !s.IsVerified).ToArray();

                if (unverifieds.Length >= settingsService.Contents.Detection.MaxCountUnverifieds)
                {
                    var relevant = unverifieds
                        .Where(s => s != Active)
                        .OrderBy(s => s.Index).FirstOrDefault();

                    RemoveSample(relevant);
                }

                if (Samples.Count >= Constants.MaxCountSamples)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Sample),
                        maxCount: Constants.MaxCountSamples);
                }

                sample.Index = index++;
                sample.Template = template;

                // Bitmap and normalized data must be assigned here rather than in the model
                // because samples loaded from saved settings also need this initialization.
                recognitionService.Bind(sample);

                ImmutableList<Sample> add(ImmutableList<Sample> c) => !c.Contains(sample)
                    ? c.Add(sample)
                    : c;

                ImmutableInterlocked.Update(
                    location: ref samples,
                    transformer: add);
            }
        }

        public void Clear()
        {
            if (Samples.Count > 0)
            {
                static ImmutableList<Sample> clear(ImmutableList<Sample> c) => c.Clear();

                ImmutableInterlocked.Update(
                    location: ref samples,
                    transformer: clear);

                SaveSamples();

                samplesChangedEvent.Publish();

                Select();
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
                var sample = CreateSample(segment);

                if (segment.Area.Template == default)
                {
                    segment.Area.Template = sample.Template;
                    segment.Area.TemplateName = sample.Template.Name;

                    templateSelectedEvent.Publish(sample.Template);
                }

                Select(sample);
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
                .Where(s => !s.IsFiltered)
                .GetNext(
                    active: Active,
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
            if (Active != default)
            {
                var result = ButtonResult.Yes;

                if (Active.IsVerified)
                {
                    result = await dialogService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the selected sample be removed?",
                        contentTitle: "Remove sample");
                }

                if (result == ButtonResult.Yes)
                {
                    var next = Samples.Count > 1
                        ? Samples.GetNext(Active)
                        : default;

                    RemoveSample(Active);

                    Select(next);
                }
            }
        }

        public void Select(Sample sample = default)
        {
            if (Active != sample)
            {
                Active = sample;

                sampleSelectedEvent.Publish(Active);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Sample CreateSample(Segment segment)
        {
            var result = default(Sample);

            if (segment?.Image != default)
            {
                result = new Sample
                {
                    Bitmap = segment.Bitmap,
                    Height = segment.Bitmap?.Size.Height ?? 0,
                    Width = segment.Bitmap?.Size.Width ?? 0,
                    Image = segment.Image,
                };

                Add(result);

                result.Value = recognitionService
                    .Detect(result)?.Value;

                SaveSamples();

                samplesChangedEvent.Publish();
            }

            return result;
        }

        private void DetectSample(Segment segment)
        {
            var samples = Samples?.ToArray();

            if (segment?.IsEmpty == false
                && !recognitionService.HasSimilars(
                    segment: segment,
                    samples: samples))
            {
                try
                {
                    CreateSample(segment);
                }
                catch (MaxCountExceededException)
                { }
            }
        }

        private void RemoveSample(Sample sample)
        {
            if (sample != default)
            {
                ImmutableList<Sample> remove(ImmutableList<Sample> c) => c.Contains(sample)
                    ? c.Remove(sample)
                    : c;

                ImmutableInterlocked.Update(
                    location: ref samples,
                    transformer: remove);

                SaveSamples();

                samplesChangedEvent.Publish();
            }
        }

        private void SaveSamples()
        {
            var untransformeds = Samples
                .Where(s => s.Image != default
                    && s.Bytes == default).ToArray();

            foreach (var untransformed in untransformeds)
            {
                untransformed.Bytes = untransformed.Image.ToBytes();
            }

            template.Samples = Samples?.ToList();

            settingsService.Save();
        }

        #endregion Private Methods
    }
}