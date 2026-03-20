using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly object samplesLock = new();
        private readonly SamplesOrderedEvent samplesOrderedEvent;
        private readonly ISettingsService<Session> settingsService;
        private readonly TemplateSelectedEvent templateSelectedEvent;

        private int index;
        private bool orderDescending;
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
        }

        #endregion Public Constructors

        #region Public Properties

        public Sample Active { get; private set; }

        public bool IsDetection { get; set; }

        public IReadOnlyList<Sample> Samples
        {
            get { lock (samplesLock) return template.Samples.ToList(); }
        }

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

                if (Samples.Count() >= Constants.MaxCountSamples)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Sample),
                        maxCount: Constants.MaxCountSamples);
                }

                sample.Index = index++;
                sample.Template = template;

                // Bitmap and normalized data must be assigned here rather than in the model
                // because samples loaded from saved settings also need this initialization.
                recognitionService.Update(sample);

                lock (samplesLock) template.Samples.Add(sample);

                samplesChangedEvent.Publish();
            }
        }

        public void Clear()
        {
            if (Samples.Any())
            {
                lock (samplesLock) template.Samples.Clear();

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
                var sample = CreateSample(segment);

                if (sample != default)
                {
                    sample.Value = recognitionService
                        .GetFromBase(sample)?.Value;

                    Select(sample);
                }
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
                    var next = Samples.GetNext(Active);

                    RemoveSample(Active);

                    Select(next);
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

                if (segment.Area.Template == default)
                {
                    segment.Area.Template = result.Template;
                    segment.Area.TemplateName = result.Template.Name;

                    templateSelectedEvent.Publish(result.Template);
                }
            }

            return result;
        }

        private void DetectSample(Segment segment)
        {
            if (segment?.IsEmpty == false
                && !recognitionService.HasSimilars(segment))
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
                lock (samplesLock) template.Samples.Remove(sample);

                samplesChangedEvent.Publish();
            }
        }

        #endregion Private Methods
    }
}