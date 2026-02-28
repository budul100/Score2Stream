using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaUI.Ribbon;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using ReactiveUI;
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
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.Commons.Prism;

namespace Score2Stream.MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly DetectionChangedEvent detectionChangedEvent;
        private readonly FilterChangedEvent filterChangedEvent;
        private readonly IInputService inputService;
        private readonly IRegionManager regionManager;
        private readonly ISettingsService<Session> settingsService;
        private readonly TabSelectedEvent tabSelectedEvent;

        private int tabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(ISettingsService<Session> settingsService, IWebService webService,
            IScoreboardService scoreboardService, IInputService inputService, IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.settingsService = settingsService;
            this.inputService = inputService;
            this.regionManager = regionManager;

            this.SelectTabCommand = new DelegateCommand<ViewType?>(
                executeMethod: t => TabIndex = (int?)t);

            this.ServerOpenCommand = new DelegateCommand(
                executeMethod: webService.OpenRoot);
            this.ServerReloadCommand = new DelegateCommand(
                executeMethod: async () => await webService.ReloadAsync());
            this.ScoreboardOpenCommand = new DelegateCommand(
                executeMethod: webService.OpenServer,
                canExecuteMethod: () => webService.IsActive);
            this.ScoreboardUpdateCommand = new DelegateCommand(
                executeMethod: scoreboardService.Update,
                canExecuteMethod: () => !scoreboardService.UpToDate);

            this.InputUpdateCommand = new DelegateCommand(
                executeMethod: UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<object>(
                executeMethod: param => inputService.SelectAsync(param as Input));
            this.InputStopCommand = new DelegateCommand(
                executeMethod: () => inputService.StopAsync(),
                canExecuteMethod: () => inputService.Active != default);

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
                executeMethod: AddSegments,
                canExecuteMethod: _ => inputService.IsActive);
            this.AreaRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.RemoveAsync(),
                canExecuteMethod: () => inputService.AreaService?.Area != default);
            this.AreaRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.ClearAsync(),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Count > 0);
            this.AreaUndoCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Undo(),
                canExecuteMethod: () => inputService.AreaService?.CanUndo == true);
            this.AreaOrderAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Order(true),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Count > 0);

            this.TemplateSelectCommand = new DelegateCommand<object>(
                executeMethod: param => SelectTemplate(param as Template));
            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.TemplateService.RemoveAsync(),
                canExecuteMethod: () => inputService?.TemplateService?.Template != default);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService?.Create(inputService.AreaService.Segment),
                canExecuteMethod: () => inputService?.AreaService?.Segment != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.RemoveAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Sample != default);
            this.SampleRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.ClearAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Count > 0);
            this.SampleOrderCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Order(true),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Count > 0);

            tabSelectedEvent = eventAggregator.GetEvent<TabSelectedEvent>();
            detectionChangedEvent = eventAggregator.GetEvent<DetectionChangedEvent>();
            filterChangedEvent = eventAggregator.GetEvent<FilterChangedEvent>();

            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: OnServerStarted);

            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: UpdateInputs,
                threadOption: ThreadOption.UIThread);
            eventAggregator.GetEvent<InputEndedEvent>().Subscribe(
                action: UpdateInputs,
                threadOption: ThreadOption.UIThread);
            eventAggregator.GetEvent<InputsChangedEvent>().Subscribe(
                action: RefreshInputs,
                threadOption: ThreadOption.UIThread);
            eventAggregator.GetEvent<InputUpdatedEvent>().Subscribe(
                action: RefreshInput);

            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: OnClipsChanged);
            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());
            eventAggregator.GetEvent<SegmentSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());

            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => OnClipsUpdated());

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: RefreshTemplates);
            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: OnSamplesChanged);
            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: _ => OnSampleSelected());

            eventAggregator.GetEvent<ScoreboardModifiedEvent>().Subscribe(
                action: () => ScoreboardUpdateCommand.RaiseCanExecuteChanged());
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

        public DelegateCommand InputRotateLeftCommand { get; }

        public DelegateCommand InputRotateRightCommand { get; }

        public ObservableCollection<RibbonDropDownItem> Inputs { get; } = [];

        public DelegateCommand<object> InputSelectCommand { get; }

        public DelegateCommand InputStopCommand { get; }

        public DelegateCommand InputUpdateCommand { get; }

        public bool IsActive => inputService.IsActive;

        public bool IsSampleDetection
        {
            get
            {
                return inputService?.SampleService?.IsDetection ?? false;
            }
            set
            {
                if (IsActive
                    && inputService?.SampleService?.IsDetection != value)
                {
                    inputService.SampleService.IsDetection = value;

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

        public DelegateCommand TemplateRemoveCommand { get; }

        public ObservableCollection<RibbonDropDownItem> Templates { get; } = [];

        public DelegateCommand<object> TemplateSelectCommand { get; }

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

        private void AddSegments(string number)
        {
            if (inputService.AreaService != default
                && int.TryParse(number, out var size)
                && size >= Constants.SegmentsCountMin
                && size <= Constants.SegmentsCountMax)
            {
                inputService.AreaService.Create(size);
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

        private void OnClipsChanged()
        {
            AreaRemoveCommand.RaiseCanExecuteChanged();
            AreaRemoveAllCommand.RaiseCanExecuteChanged();
            AreaOrderAllCommand.RaiseCanExecuteChanged();

            SampleAddCommand.RaiseCanExecuteChanged();
        }

        private void OnClipsUpdated()
        {
            AreaUndoCommand.RaiseCanExecuteChanged();
            AreaOrderAllCommand.RaiseCanExecuteChanged();

            RefreshTemplates();
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
            RefreshTemplates();

            TemplateRemoveCommand.RaiseCanExecuteChanged();
            SampleAddCommand.RaiseCanExecuteChanged();

            OnSamplesChanged();
        }

        private void RefreshInput()
        {
            InputStopCommand.RaiseCanExecuteChanged();

            InputCenterCommand.RaiseCanExecuteChanged();
            InputRotateLeftCommand.RaiseCanExecuteChanged();
            InputRotateRightCommand.RaiseCanExecuteChanged();

            AreaAddCommand.RaiseCanExecuteChanged();
        }

        private void RefreshInputs()
        {
            var menuInputs = new HashSet<Input>(Inputs
                .Where(i => i.CommandParameter != default)
                .Select(i => (Input)i.CommandParameter));

            var serviceInputs = new HashSet<Input>(inputService.Inputs);

            if (!menuInputs.SetEquals(serviceInputs))
            {
                Inputs.Clear();

                var ordereds = inputService.Inputs
                    .Where(i => i != default)
                    .OrderByDescending(i => i.IsDevice)
                    .ThenBy(i => i.Name).ToArray();

                foreach (var ordered in ordereds)
                {
                    var isChecked = (ordered.IsDevice && ordered.IsActive)
                        || (!ordered.IsDevice && !ordered.IsEnded);

                    var input = new RibbonDropDownItem
                    {
                        Command = InputSelectCommand,
                        CommandParameter = ordered,
                        IsChecked = isChecked,
                        Text = ordered.Name
                    };

                    Inputs.Add(input);
                }

                var fileInput = new RibbonDropDownItem
                {
                    Command = InputSelectCommand,
                    Text = Texts.MenuInputFileText,
                };

                Inputs.Add(fileInput);

                RaisePropertyChanged(nameof(Inputs));
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(NoCropping));
                RaisePropertyChanged(nameof(ProcessingDelay));
                RaisePropertyChanged(nameof(ThresholdDetecting));
                RaisePropertyChanged(nameof(ThresholdMatching));
                RaisePropertyChanged(nameof(WaitingDuration));

                OnSamplesChanged();
            }
            else
            {
                var relevants = Inputs
                    .Where(i => i?.CommandParameter != default).ToArray();

                foreach (var relevant in relevants)
                {
                    if (relevant.CommandParameter is Input currentInput)
                    {
                        relevant.Text = currentInput.Name;
                        relevant.IsChecked = (currentInput.IsDevice && currentInput.IsActive)
                            || (!currentInput.IsDevice && !currentInput.IsEnded);
                    }
                }

                RaisePropertyChanged(nameof(Inputs));
                RaisePropertyChanged(nameof(IsActive));
            }

            InputStopCommand.RaiseCanExecuteChanged();
        }

        private void RefreshTemplates()
        {
            Templates.Clear();

            if (inputService.TemplateService != default)
            {
                var ordereds = inputService.TemplateService.Templates
                    .OrderBy(t => t.Name).ToArray();

                foreach (var ordered in ordereds)
                {
                    var isChecked = ordered == inputService.TemplateService.Template;

                    var template = new RibbonDropDownItem
                    {
                        Command = TemplateSelectCommand,
                        CommandParameter = ordered,
                        IsChecked = isChecked,
                        Text = ordered.Name,
                    };

                    Templates.Add(template);
                }

                var selectTemplateAdd = new RibbonDropDownItem
                {
                    Command = TemplateSelectCommand,
                    Text = Texts.MenuTemplateAddText,
                };

                Templates.Add(selectTemplateAdd);

                RaisePropertyChanged(nameof(Templates));
            }
        }

        private void SelectTemplate(Template template)
        {
            if (inputService?.TemplateService != default)
            {
                if (template == default)
                {
                    inputService.TemplateService.Create();
                }
                else
                {
                    inputService.TemplateService.Select(template);
                }
            }
        }

        private void UpdateInputs()
        {
            inputService.Update();

            RefreshInputs();
        }

        #endregion Private Methods
    }
}