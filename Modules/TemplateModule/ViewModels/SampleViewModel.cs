using Core.Events.Samples;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using Core.Prism;
using Prism.Commands;
using Prism.Events;
using System.Windows.Media.Imaging;

namespace TemplateModule.ViewModels
{
    public class SampleViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private bool isActive;
        private ISampleService sampleService;

        #endregion Private Fields

        #region Public Constructors

        public SampleViewModel(IEventAggregator eventAggregator)
        {
            OnFocusCommand = new DelegateCommand(
                executeMethod: () => sampleService.Select(Sample));

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsActive = s == Sample,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => RaisePropertyChanged(nameof(Difference)),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Bitmap => Sample?.Bitmap;

        public string Difference => Sample?.Image != default
            ? $"Difference: {(int)(Sample.Similarity * 100)}"
            : default;

        public bool HasNoValue => string.IsNullOrWhiteSpace(Value);

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public DelegateCommand OnFocusCommand { get; }

        public Sample Sample { get; private set; }

        public string Value
        {
            get { return Sample?.Value; }
            set
            {
                Sample.Value = value;

                RaisePropertyChanged(nameof(Value));
                RaisePropertyChanged(nameof(HasNoValue));
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