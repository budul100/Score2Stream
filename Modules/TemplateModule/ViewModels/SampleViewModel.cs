using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class SampleViewModel
        : BindableBase
    {
        #region Private Fields

        private bool isActive;
        private TemplateViewModel parent;
        private ISampleService sampleService;

        #endregion Private Fields

        #region Public Constructors

        public SampleViewModel(IEventAggregator eventAggregator)
        {
            OnSelectionCommand = new DelegateCommand(
                executeMethod: () => sampleService.Select(Sample));

            OnSelectionNextCommand = new DelegateCommand(
                executeMethod: () => parent.SelectNext(true));

            OnSelectionPreviousCommand = new DelegateCommand(
                executeMethod: () => parent.SelectNext(false));

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsActive = s == Sample,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => RaisePropertyChanged(nameof(Difference)),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => Sample?.Bitmap;

        public string Difference => Sample?.Image != default
            ? $"Difference: {(int)(Sample.Similarity * 100)}"
            : default;

        public bool HasNoValue => string.IsNullOrWhiteSpace(Value);

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

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
                RaisePropertyChanged(nameof(HasNoValue));
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Sample sample, ISampleService sampleService, TemplateViewModel parent)
        {
            this.Sample = sample;
            this.sampleService = sampleService;
            this.parent = parent;

            Value = sample.Value;

            RaisePropertyChanged(nameof(Bitmap));
        }

        #endregion Public Methods
    }
}