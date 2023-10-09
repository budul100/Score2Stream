using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Core.Enums;
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

        private readonly SampleUpdatedEvent sampleUpdatedEvent;

        private bool isActive;
        private ISampleService sampleService;

        #endregion Private Fields

        #region Public Constructors

        public SampleViewModel(IEventAggregator eventAggregator)
        {
            OnRemoveCommand = new DelegateCommand(
                executeMethod: () => sampleService.RemoveAsync());

            OnFocusGotCommand = new DelegateCommand(
                executeMethod: () => sampleService.Select(Sample));

            OnFocusLostCommand = new DelegateCommand(
                executeMethod: () => Sample.IsVerified = true);

            OnSelectionNextCommand = new DelegateCommand(
                executeMethod: () => sampleService.Next(false));

            OnSelectionPreviousCommand = new DelegateCommand(
                executeMethod: () => sampleService.Next(true));

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsActive = s == Sample,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesUpdatedEvent>().Subscribe(
                action: () => RaisePropertyChanged(nameof(Type)),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => RaisePropertyChanged(nameof(Difference)),
                keepSubscriberReferenceAlive: true);

            sampleUpdatedEvent = eventAggregator
                .GetEvent<SampleUpdatedEvent>();
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

        public DelegateCommand OnFocusGotCommand { get; }

        public DelegateCommand OnFocusLostCommand { get; }

        public DelegateCommand OnRemoveCommand { get; }

        public DelegateCommand OnSelectionNextCommand { get; }

        public DelegateCommand OnSelectionPreviousCommand { get; }

        public Sample Sample { get; private set; }

        public SampleType Type => Sample.Type;

        public string Value
        {
            get { return Sample?.Value; }
            set
            {
                Sample.Value = value;

                RaisePropertyChanged(nameof(Value));

                sampleUpdatedEvent.Publish(Sample);
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
    }
}