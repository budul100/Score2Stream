using Prism.Commands;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Segment;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;

namespace Score2Stream.MenuModule.ViewModels
{
    public partial class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private DetectionChangedEvent detectionChangedEvent;
        private FilterChangedEvent filterChangedEvent;
        private ITemplateService templateService;

        #endregion Private Fields

        #region Public Properties

        public static string TabTemplates => Constants.TabTemplates;

        public static int UnverifiedsCountMax => Constants.MaxCountSamples;

        public bool IsActiveSample => templateService?.Active != default;

        public bool IsSampleDetection
        {
            get
            {
                return templateService?.SampleService?.IsDetection ?? false;
            }
            set
            {
                if (IsActive
                    && templateService?.SampleService != default
                    && templateService.SampleService.IsDetection != value)
                {
                    templateService.SampleService.IsDetection = value;

                    detectionChangedEvent.Publish();

                    RaisePropertyChanged(nameof(IsSampleDetection));
                }
            }
        }

        public bool IsVerifiedsFiltered
        {
            get
            {
                return settingsService?.Contents?.Detection?.FilterVerifieds ?? false;
            }
            set
            {
                if (settingsService.Contents.Detection.FilterVerifieds != value)
                {
                    settingsService.Contents.Detection.FilterVerifieds = value;

                    filterChangedEvent.Publish();

                    RaisePropertyChanged(nameof(IsVerifiedsFiltered));
                }
            }
        }

        public DelegateCommand SampleAddCommand { get; private set; }

        public DelegateCommand SampleOrderCommand { get; private set; }

        public DelegateCommand SampleRemoveAllCommand { get; private set; }

        public DelegateCommand SampleRemoveCommand { get; private set; }

        public DelegateCommand TemplateAddCommand { get; private set; }

        public int ThresholdDetecting
        {
            get { return settingsService.Contents.Detection.ThresholdDetecting; }
            set
            {
                if (value >= 0
                    && value <= ThresholdMax
                    && settingsService.Contents.Detection.ThresholdDetecting != value)
                {
                    settingsService.Contents.Detection.ThresholdDetecting = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ThresholdDetecting));
                }
            }
        }

        public int UnverifiedsCount
        {
            get { return settingsService.Contents.Detection.MaxCountUnverifieds; }
            set
            {
                if (value >= 0
                    && value <= UnverifiedsCountMax
                    && settingsService.Contents.Detection.MaxCountUnverifieds != value)
                {
                    settingsService.Contents.Detection.MaxCountUnverifieds = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(UnverifiedsCount));
                }
            }
        }

        public int WaitingDuration
        {
            get { return settingsService.Contents.Detection.DurationDetectionWait; }
            set
            {
                if (value >= 0
                    && value <= DelayMax
                    && settingsService.Contents.Detection.DurationDetectionWait != value)
                {
                    settingsService.Contents.Detection.DurationDetectionWait = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(WaitingDuration));
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void AddTemplate()
        {
            if (templateService != default)
            {
                templateService.Create();
            }
        }

        private bool CanAddSample()
        {
            return inputService?.AreaService?.ActiveSegment != default
                && (templateService?.Active?.Samples?.Count <= Constants.MaxCountSamples) == true;
        }

        private bool CanAddTemplate()
        {
            return (templateService?.Templates?.Count <= Constants.MaxCountTemplates) == true;
        }

        private partial void InitializeViewTemplate(ITemplateService templateService, IEventAggregator eventAggregator)
        {
            this.templateService = templateService;

            this.TemplateAddCommand = new DelegateCommand(
                executeMethod: AddTemplate,
                canExecuteMethod: CanAddTemplate);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService?.Create(inputService.AreaService.ActiveSegment),
                canExecuteMethod: CanAddSample);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService.RemoveAsync(),
                canExecuteMethod: () => templateService?.SampleService?.Active != default);
            this.SampleRemoveAllCommand = new DelegateCommand(
                executeMethod: () => templateService.SampleService.ClearAsync(),
                canExecuteMethod: () => templateService?.SampleService?.Samples?.Count > 0);
            this.SampleOrderCommand = new DelegateCommand(
                executeMethod: () => templateService.SampleService.Order(true),
                canExecuteMethod: () => templateService?.SampleService?.Samples?.Count > 0);

            detectionChangedEvent = eventAggregator.GetEvent<DetectionChangedEvent>();
            filterChangedEvent = eventAggregator.GetEvent<FilterChangedEvent>();

            eventAggregator.GetEvent<SegmentSelectedEvent>().Subscribe(
                action: _ => OnAreasChanged());

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());
            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: OnTemplateChanged);

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: _ => OnSampleSelected());
            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: OnSamplesChanged);
        }

        private void OnSamplesChanged()
        {
            SampleAddCommand.RaiseCanExecuteChanged();

            SampleRemoveCommand.RaiseCanExecuteChanged();
            SampleRemoveAllCommand.RaiseCanExecuteChanged();

            SampleOrderCommand.RaiseCanExecuteChanged();
        }

        private void OnSampleSelected()
        {
            SampleRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnTemplateChanged()
        {
            SampleAddCommand.RaiseCanExecuteChanged();

            OnSamplesChanged();
        }

        private void OnTemplateSelected()
        {
            RaisePropertyChanged(nameof(IsActiveSample));

            OnTemplateChanged();
        }

        #endregion Private Methods
    }
}