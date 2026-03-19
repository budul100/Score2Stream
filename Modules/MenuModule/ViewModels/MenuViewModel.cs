using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaUI.Ribbon;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.Commons.Prism;

namespace Score2Stream.MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly DetectionChangedEvent detectionChangedEvent;
        private readonly IDialogService dialogService;
        private readonly FilterChangedEvent filterChangedEvent;
        private readonly IInputService inputService;
        private readonly IRegionManager regionManager;
        private readonly IScoreboardService scoreboardService;
        private readonly ISettingsService<Session> settingsService;
        private readonly Task startLocationTask;
        private readonly TabSelectedEvent tabSelectedEvent;
        private readonly ITemplateService templateService;

        private IStorageFolder startLocation;
        private int tabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(ISettingsService<Session> settingsService, IWebService webService,
            IScoreboardService scoreboardService, IInputService inputService, ITemplateService templateService,
            IRegionManager regionManager, IDialogService dialogService, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.settingsService = settingsService;
            this.scoreboardService = scoreboardService;
            this.inputService = inputService;
            this.templateService = templateService;
            this.regionManager = regionManager;
            this.dialogService = dialogService;

            this.SelectTabCommand = new DelegateCommand<ViewType?>(
                executeMethod: t => TabIndex = (int?)t);

            this.InputRefreshCommand = new DelegateCommand(
                executeMethod: RefreshInputs);
            this.InputSelectCommand = new DelegateCommand<string>(
                executeMethod: async param => await SelectInputAsync(param));

            this.InputCenterCommand = new DelegateCommand(
                executeMethod: () => eventAggregator.GetEvent<InputCenteringEvent>().Publish(),
                canExecuteMethod: () => inputService.IsActive);
            this.InputRotateLeftCommand = new DelegateCommand(
                executeMethod: () => ChangeInputRotate(true),
                canExecuteMethod: CanRotateLeft);
            this.InputRotateRightCommand = new DelegateCommand(
                executeMethod: () => ChangeInputRotate(false),
                canExecuteMethod: CanRotateRight);

            this.AreaAddCommand = new DelegateCommand<string>(
                executeMethod: AddArea,
                canExecuteMethod: _ => inputService.IsActive);
            this.AreaRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.RemoveAsync(),
                canExecuteMethod: () => inputService.AreaService?.Active != default);
            this.AreaRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.ClearAsync(),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Count > 0);
            this.AreaUndoCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Undo(),
                canExecuteMethod: () => inputService.AreaService?.CanUndo == true);
            this.AreaOrderAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Order(true),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Count > 0);

            this.TemplateAddCommand = new DelegateCommand(
                executeMethod: AddTemplate);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService?.Create(inputService.AreaService.Segment),
                canExecuteMethod: () => inputService?.AreaService?.Segment != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => templateService?.SampleService.RemoveAsync(),
                canExecuteMethod: () => templateService?.SampleService?.Active != default);
            this.SampleRemoveAllCommand = new DelegateCommand(
                executeMethod: () => templateService.SampleService.ClearAsync(),
                canExecuteMethod: () => templateService?.SampleService?.Samples?.Count > 0);
            this.SampleOrderCommand = new DelegateCommand(
                executeMethod: () => templateService.SampleService.Order(true),
                canExecuteMethod: () => templateService?.SampleService?.Samples?.Count > 0);

            this.ServerOpenCommand = new DelegateCommand(
                executeMethod: webService.OpenRoot);
            this.ServerReloadCommand = new DelegateCommand(
                executeMethod: async () => await webService.ReloadAsync());
            this.ScoreboardOpenCommand = new DelegateCommand(
                executeMethod: webService.OpenServer,
                canExecuteMethod: () => webService.IsActive);
            this.ScoreboardUpdateCommand = new DelegateCommand(
                executeMethod: scoreboardService.Update,
                canExecuteMethod: () => !scoreboardService.IsUpToDate);

            tabSelectedEvent = eventAggregator.GetEvent<TabSelectedEvent>();
            detectionChangedEvent = eventAggregator.GetEvent<DetectionChangedEvent>();
            filterChangedEvent = eventAggregator.GetEvent<FilterChangedEvent>();

            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: OnServerStarted);
            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => OnInputChanged());
            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: _ => OnInputChanged());

            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: OnAreasChanged);
            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: _ => OnAreasChanged());
            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => OnAreasModified());

            eventAggregator.GetEvent<SegmentSelectedEvent>().Subscribe(
                action: _ => OnAreasChanged());

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: OnSamplesChanged);
            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: _ => OnSampleSelected());

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(IsUpToDate)));
            eventAggregator.GetEvent<ScoreboardModifiedEvent>().Subscribe(
                action: () => ScoreboardUpdateCommand.RaiseCanExecuteChanged());

            startLocationTask = InitializeStartLocationAsync();
        }

        #endregion Public Constructors

        #region Public Properties

        public static int DelayMax => Constants.DelayMax;

        public static int DelayMin => Constants.DelayMin;

        public static int PortMax => Constants.PortMax;

        public static int PortMin => Constants.PortMin;

        public static int QueueSizeMax => Constants.ImageQueueSizeMax;

        public static int QueueSizeMin => Constants.ImageQueueSizeMin;

        public static string TabBoard => Constants.TabBoard;

        public static string TabSamples => Constants.TabSamples;

        public static string TabSegments => Constants.TabSegments;

        public static int ThresholdMax => Constants.ThresholdMax;

        public static int UnverifiedsCountMax => Constants.MaxCountSamples;

        public bool AllowMultipleInstances
        {
            get { return settingsService.Contents.App.AllowMultipleInstances; }
            set
            {
                if (settingsService.Contents.App.AllowMultipleInstances != value)
                {
                    settingsService.Contents.App.AllowMultipleInstances = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(AllowMultipleInstances));
                }
            }
        }

        public DelegateCommand<string> AreaAddCommand { get; }

        public DelegateCommand AreaOrderAllCommand { get; }

        public DelegateCommand AreaRemoveAllCommand { get; }

        public DelegateCommand AreaRemoveCommand { get; }

        public DelegateCommand AreaUndoCommand { get; }

        public int DelaySocket
        {
            get { return settingsService.Contents.Server.DelaySocket; }
            set
            {
                if (value >= DelayMin
                    && value <= DelayMax
                    && settingsService.Contents.Server.DelaySocket != value)
                {
                    settingsService.Contents.Server.DelaySocket = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(DelaySocket));
                }
            }
        }

        public int ImagesQueueSize
        {
            get { return settingsService.Contents.Video.ImagesQueueSize; }
            set
            {
                if (value >= QueueSizeMin
                    && value <= QueueSizeMax
                    && value != settingsService.Contents.Video.ImagesQueueSize)
                {
                    settingsService.Contents.Video.ImagesQueueSize = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ImagesQueueSize));
                }
            }
        }

        public DelegateCommand InputCenterCommand { get; }

        public DelegateCommand InputRefreshCommand { get; }

        public DelegateCommand InputRotateLeftCommand { get; }

        public DelegateCommand InputRotateRightCommand { get; }

        public ObservableCollection<RibbonDropDownItem> Inputs { get; } = [];

        public DelegateCommand<string> InputSelectCommand { get; }

        public bool IsActive => inputService.IsActive;

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

        public bool IsUpToDate => scoreboardService.IsUpToDate;

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

        public bool NoCropping
        {
            get { return settingsService.Contents.Video.NoCropping; }
            set
            {
                if (settingsService.Contents.Video.NoCropping != value)
                {
                    settingsService.Contents.Video.NoCropping = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(NoCropping));
                }
            }
        }

        public int PortServer
        {
            get { return settingsService.Contents.Server.PortServer; }
            set
            {
                if (value >= PortMin
                    && value <= PortMax
                    && settingsService.Contents.Server.PortServer != value)
                {
                    settingsService.Contents.Server.PortServer = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(PortServer));
                }
            }
        }

        public int PortSocket
        {
            get { return settingsService.Contents.Server.PortSocket; }
            set
            {
                if (value >= PortMin
                    && value <= PortMax
                    && settingsService.Contents.Server.PortSocket != value)
                {
                    settingsService.Contents.Server.PortSocket = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(PortSocket));
                }
            }
        }

        public int ProcessingDelay
        {
            get { return settingsService.Contents.Video.ProcessingDelay; }
            set
            {
                if (value >= 0
                    && value <= DelayMax
                    && settingsService.Contents.Video.ProcessingDelay != value)
                {
                    settingsService.Contents.Video.ProcessingDelay = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ProcessingDelay));
                }
            }
        }

        public DelegateCommand SampleAddCommand { get; }

        public DelegateCommand SampleOrderCommand { get; }

        public DelegateCommand SampleRemoveAllCommand { get; }

        public DelegateCommand SampleRemoveCommand { get; }

        public DelegateCommand ScoreboardOpenCommand { get; }

        public DelegateCommand ScoreboardUpdateCommand { get; }

        public DelegateCommand<ViewType?> SelectTabCommand { get; }

        public DelegateCommand ServerOpenCommand { get; }

        public DelegateCommand ServerReloadCommand { get; }

        public int? TabIndex
        {
            get { return tabIndex; }
            set
            {
                if (value.HasValue
                    && TabIndex != value)
                {
                    SetProperty(ref tabIndex, value.Value);

                    switch (tabIndex)
                    {
                        case (int)ViewType.Inputs:

                            IsSampleDetection = false;

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Inputs));

                            tabSelectedEvent.Publish(ViewType.Inputs);

                            break;

                        case (int)ViewType.Templates:

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Templates));

                            tabSelectedEvent.Publish(ViewType.Templates);

                            OnSamplesChanged();

                            break;

                        case (int)ViewType.Board:

                            IsSampleDetection = false;

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Board));

                            tabSelectedEvent.Publish(ViewType.Board);

                            break;
                    }
                }
            }
        }

        public DelegateCommand TemplateAddCommand { get; }

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

        public int ThresholdMatching
        {
            get { return settingsService.Contents.Detection.ThresholdMatching; }
            set
            {
                if (value >= 0
                    && value <= ThresholdMax
                    && settingsService.Contents.Detection.ThresholdMatching != value)
                {
                    settingsService.Contents.Detection.ThresholdMatching = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ThresholdMatching));
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

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void AddArea(string numberSegments)
        {
            if (inputService.AreaService != default
                && int.TryParse(
                    s: numberSegments,
                    result: out var size)
                && size >= Constants.SegmentsCountMin
                && size <= Constants.SegmentsCountMax)
            {
                inputService.AreaService.Create(size);
            }
        }

        private void AddTemplate()
        {
            if (templateService != default)
            {
                templateService.Create();
            }
        }

        private bool CanRotateLeft()
        {
            var result = inputService.IsActive
                && inputService.Rotation >= Constants.RotateLeftMax;

            return result;
        }

        private bool CanRotateRight()
        {
            var result = inputService.IsActive
                && inputService.Rotation <= Constants.RotateRightMax;

            return result;
        }

        private void ChangeInputRotate(bool toLeft)
        {
            if (toLeft)
            {
                if (CanRotateLeft())
                {
                    inputService.Rotation -= Constants.RotateStep;
                }
            }
            else
            {
                if (CanRotateRight())
                {
                    inputService.Rotation += Constants.RotateStep;
                }
            }
        }

        private async Task<string> GetInputFileAsync()
        {
            if (Inputs.Count >= Constants.MaxCountInputs)
            {
                await dialogService.ShowMessageBoxAsync(
                    contentMessage: $"Maximum number of {Constants.MaxCountInputs} inputs is exceeded.",
                    contentTitle: "Maximum inputs exceeded",
                    icon: Icon.Error);

                return default;
            }

            if (startLocationTask != null)
            {
                await startLocationTask;
            }

            var paths = await dialogService.OpenFilePickerAsync(
                title: Texts.MenuInputFileText,
                allowMultiple: false,
                startLocation: startLocation);

            if (paths?.Any() != true) return default;

            var result = paths
                .Select(p => p.Path.LocalPath)
                .FirstOrDefault(File.Exists);

            if (string.IsNullOrWhiteSpace(result)) return default;

            startLocation = await dialogService.GetFolderAsync(result);

            settingsService.Contents.Video.FilePathVideo = result;
            settingsService.Save();

            return result;
        }

        private async Task InitializeStartLocationAsync()
        {
            try
            {
                var filePathVideo = settingsService.Contents.Video.FilePathVideo;

                startLocation = await dialogService.GetFolderAsync(filePathVideo);
            }
            catch { }
        }

        private void OnAreasChanged()
        {
            AreaRemoveCommand.RaiseCanExecuteChanged();
            AreaRemoveAllCommand.RaiseCanExecuteChanged();
            AreaOrderAllCommand.RaiseCanExecuteChanged();

            SampleAddCommand.RaiseCanExecuteChanged();
        }

        private void OnAreasModified()
        {
            AreaUndoCommand.RaiseCanExecuteChanged();
            AreaOrderAllCommand.RaiseCanExecuteChanged();
        }

        private void OnInputChanged()
        {
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(IsActiveSample));
            RaisePropertyChanged(nameof(NoCropping));
            RaisePropertyChanged(nameof(ProcessingDelay));
            RaisePropertyChanged(nameof(ThresholdDetecting));
            RaisePropertyChanged(nameof(ThresholdMatching));
            RaisePropertyChanged(nameof(WaitingDuration));

            InputCenterCommand.RaiseCanExecuteChanged();
            InputRotateLeftCommand.RaiseCanExecuteChanged();
            InputRotateRightCommand.RaiseCanExecuteChanged();

            AreaAddCommand.RaiseCanExecuteChanged();
        }

        private void OnSamplesChanged()
        {
            SampleRemoveAllCommand.RaiseCanExecuteChanged();
            SampleOrderCommand.RaiseCanExecuteChanged();
        }

        private void OnSampleSelected()
        {
            SampleRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnServerStarted()
        {
            ServerOpenCommand.RaiseCanExecuteChanged();
            ServerReloadCommand.RaiseCanExecuteChanged();

            ScoreboardOpenCommand.RaiseCanExecuteChanged();
            ScoreboardUpdateCommand.RaiseCanExecuteChanged();
        }

        private void OnTemplateSelected()
        {
            RaisePropertyChanged(nameof(IsActiveSample));

            SampleAddCommand.RaiseCanExecuteChanged();

            OnSamplesChanged();
        }

        private void RefreshInputs()
        {
            Inputs.Clear();

            var devices = inputService.GetDevices();

            var ordereds = devices
                .OrderBy(i => i.Value).ToArray();

            foreach (var ordered in ordereds)
            {
                var input = new RibbonDropDownItem
                {
                    Command = InputSelectCommand,
                    CommandParameter = ordered.Value,
                    Text = ordered.Value
                };

                Inputs.Add(input);
            }

            var fileInput = new RibbonDropDownItem
            {
                Command = InputSelectCommand,
                Text = Texts.MenuInputFileText,
            };

            Inputs.Add(fileInput);
        }

        private async Task SelectInputAsync(string deviceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    var fileName = await GetInputFileAsync();
                    inputService.SelectFile(fileName);
                }
                else
                {
                    inputService.SelectDevice(deviceName);
                }
            }
            catch (MaxCountExceededException exception)
            {
                await dialogService.ShowMessageBoxAsync(
                    contentMessage: exception.Message,
                    contentTitle: "Maximum count exceeded",
                    icon: Icon.Error);

                return;
            }
        }

        #endregion Private Methods
    }
}