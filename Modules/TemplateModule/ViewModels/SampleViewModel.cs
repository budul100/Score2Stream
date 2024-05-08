using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class SampleViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly SampleModifiedEvent sampleModifiedEvent;

        private IAreaService areaService;
        private bool isSelected;
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
                executeMethod: () => SetVerified());

            OnSelectionCommand = new DelegateCommand(
                executeMethod: () => sampleService.Select(Sample));
            OnSelectionNextCommand = new DelegateCommand(
                executeMethod: () => sampleService.Next(false));
            OnSelectionPreviousCommand = new DelegateCommand(
                executeMethod: () => sampleService.Next(true));

            sampleModifiedEvent = eventAggregator.GetEvent<SampleModifiedEvent>();

            eventAggregator.GetEvent<SegmentSelectedEvent>().Subscribe(
                action: _ => UpdateValues(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsSelected = s == Sample,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SampleUpdatedEvent>().Subscribe(
                action: _ => UpdateValues(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == Sample);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => Sample.Bitmap;

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                SetProperty(ref isSelected, value);
                RaisePropertyChanged(nameof(IsUnverified));
            }
        }

        public bool IsUnverified => !Sample.IsVerified;

        public DelegateCommand OnFocusGotCommand { get; }

        public DelegateCommand OnFocusLostCommand { get; }

        public DelegateCommand OnRemoveCommand { get; }

        public DelegateCommand OnSelectionCommand { get; }

        public DelegateCommand OnSelectionNextCommand { get; }

        public DelegateCommand OnSelectionPreviousCommand { get; }

        public Sample Sample { get; private set; }

        public string Similarity => areaService?.Segment != default
            ? $"Similarity: {(int)(Sample.Similarity * Constants.ThresholdDivider)}%"
            : default;

        public SampleType Type => areaService?.Segment != default
            ? Sample.Type
            : SampleType.None;

        public string Value
        {
            get { return Sample?.Value; }
            set
            {
                Sample.Value = value;

                RaisePropertyChanged(nameof(Value));

                sampleModifiedEvent.Publish(Sample);
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Sample sample, IInputService inputService)
        {
            this.Sample = sample;
            this.areaService = inputService.AreaService;
            this.sampleService = inputService.SampleService;

            Value = sample.Value;

            RaisePropertyChanged(nameof(Bitmap));
        }

        #endregion Public Methods

        #region Private Methods

        private void SetVerified()
        {
            if (!Sample.IsVerified)
            {
                Sample.IsVerified = true;

                RaisePropertyChanged(nameof(IsUnverified));
            }
        }

        private void UpdateValues()
        {
            RaisePropertyChanged(nameof(Similarity));
            RaisePropertyChanged(nameof(Type));
        }

        #endregion Private Methods
    }
}