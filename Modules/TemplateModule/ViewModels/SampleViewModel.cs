using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class SampleViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly SampleModifiedEvent sampleModifiedEvent;
        private readonly ISettingsService<Session> settingsService;

        private bool isSelected;
        private ITemplateService templateService;

        #endregion Private Fields

        #region Public Constructors

        public SampleViewModel(ISettingsService<Session> settingsService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;

            OnRemoveCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService?.RemoveAsync());

            OnFocusGotCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService?.Select(Sample));
            OnFocusLostCommand = new DelegateCommand(
                executeMethod: SetVerified);

            OnSelectionCommand = new DelegateCommand(
                executeMethod: SelectSample);
            OnSelectionNextCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService?.Next(false));
            OnSelectionPreviousCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService?.Next(true));

            sampleModifiedEvent = eventAggregator.GetEvent<SampleModifiedEvent>();

            eventAggregator.GetEvent<SampleUpdatedEvent>().Subscribe(
                action: _ => UpdateMatch(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == Sample);

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsSelected = s == Sample,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<FilterChangedEvent>().Subscribe(
                action: () => RaisePropertyChanged(nameof(IsVisible)),
                keepSubscriberReferenceAlive: true);
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

        public bool IsVisible => !settingsService.Contents.Detection.FilterVerifieds || !Sample.IsFiltered;

        public DelegateCommand OnFocusGotCommand { get; }

        public DelegateCommand OnFocusLostCommand { get; }

        public DelegateCommand OnRemoveCommand { get; }

        public DelegateCommand OnSelectionCommand { get; }

        public DelegateCommand OnSelectionNextCommand { get; }

        public DelegateCommand OnSelectionPreviousCommand { get; }

        public Sample Sample { get; private set; }

        public string Similarity => Sample?.Match?.Similarity > 0
            ? $"Similarity: {(int)(Sample.Match.Similarity * Constants.ThresholdDivider)}%"
            : default;

        public MatchType Type => (Sample.Match?.Type) ?? MatchType.None;

        public string Value
        {
            get { return Sample?.Value; }
            set
            {
                if (Sample.Value != value)
                {
                    Sample.Value = value;

                    RaisePropertyChanged(nameof(Value));

                    sampleModifiedEvent.Publish(Sample);
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Sample sample, ITemplateService templateService)
        {
            Sample = sample;
            Value = sample.Value;

            this.templateService = templateService;

            RaisePropertyChanged(nameof(Bitmap));
        }

        #endregion Public Methods

        #region Private Methods

        private void SelectSample()
        {
            if (IsSelected)
            {
                SetVerified();
            }

            templateService.SampleService.Select(Sample);
        }

        private void SetVerified()
        {
            if (!Sample.IsVerified)
            {
                Sample.IsVerified = true;

                RaisePropertyChanged(nameof(IsUnverified));
            }
        }

        private void UpdateMatch()
        {
            RaisePropertyChanged(nameof(Similarity));
            RaisePropertyChanged(nameof(Type));
        }

        #endregion Private Methods
    }
}