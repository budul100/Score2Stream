using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class SampleViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly SampleUpdatedValueEvent sampleUpdatedValueEvent;
        private bool isActive;
        private bool isMatching;
        private bool isRelevant;
        private ISampleService sampleService;

        #endregion Private Fields

        #region Public Constructors

        public SampleViewModel(IEventAggregator eventAggregator)
        {
            OnRemoveCommand = new DelegateCommand(
                executeMethod: () => sampleService.RemoveAsync());

            OnSelectionCommand = new DelegateCommand(
                executeMethod: () => sampleService.Select(Sample));

            OnSelectionNextCommand = new DelegateCommand(
                executeMethod: () => sampleService.Next(true));

            OnSelectionPreviousCommand = new DelegateCommand(
                executeMethod: () => sampleService.Next(false));

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsActive = s == Sample,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SampleUpdatedRelevanceEvent>().Subscribe(
                action: (s) => UpdateSample(s),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => RaisePropertyChanged(nameof(Difference)),
                keepSubscriberReferenceAlive: true);

            sampleUpdatedValueEvent = eventAggregator
                .GetEvent<SampleUpdatedValueEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => Sample?.Bitmap;

        public string Difference => Sample?.Mat != default
            ? $"Similarity: {(int)(Sample.Similarity * 100)}%"
            : default;

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public bool IsMatching
        {
            get { return isMatching; }
            set { SetProperty(ref isMatching, value); }
        }

        public bool IsRelevant
        {
            get { return isRelevant; }
            set { SetProperty(ref isRelevant, value); }
        }

        public DelegateCommand OnRemoveCommand { get; }

        public DelegateCommand OnSelectionCommand { get; }

        public DelegateCommand OnSelectionNextCommand { get; }

        public DelegateCommand OnSelectionPreviousCommand { get; }

        public Sample Sample { get; private set; }

        public string Value
        {
            get { return Sample?.Value; }
            set
            {
                Sample.Value = value;

                RaisePropertyChanged(nameof(Value));

                sampleUpdatedValueEvent.Publish(Sample);
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Sample sample, ISampleService sampleService)
        {
            this.Sample = sample;
            this.sampleService = sampleService;

            Value = sample.Value;

            RaisePropertyChanged(nameof(Bitmap));
        }

        #endregion Public Methods

        #region Private Methods

        private void UpdateSample(Sample sample)
        {
            if (sample == Sample)
            {
                IsMatching = sample.IsSimilar;
                IsRelevant = sample.IsMatching;
            }
        }

        #endregion Private Methods
    }
}