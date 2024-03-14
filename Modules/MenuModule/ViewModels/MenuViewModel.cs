using System;
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
using Score2Stream.Commons.Events.Video;
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

            this.GraphicsReloadCommand = new DelegateCommand(
                executeMethod: async () => await webService.ReloadAsync());
            this.ScoreboardOpenCommand = new DelegateCommand(
                executeMethod: () => webService.Open(),
                canExecuteMethod: () => webService.IsActive);
            this.ScoreboardUpdateCommand = new DelegateCommand(
                executeMethod: () => scoreboardService.Update(),
                canExecuteMethod: () => !scoreboardService.UpToDate);

            this.InputsUpdateCommand = new DelegateCommand(
                executeMethod: UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<Input>(
                executeMethod: i => inputService.SelectAsync(i));
            this.InputStopAllCommand = new DelegateCommand(
                executeMethod: () => inputService.StopAsync(),
                canExecuteMethod: () => inputService.IsActive);

            this.InputCenterCommand = new DelegateCommand(
                executeMethod: () => eventAggregator.GetEvent<CenteringRequestedEvent>().Publish(),
                canExecuteMethod: () => inputService.IsActive);
            this.InputRotateLeftCommand = new DelegateCommand(
                executeMethod: () => ChangeInputRotate(true),
                canExecuteMethod: () => CanRotateLeft());
            this.InputRotateRightCommand = new DelegateCommand(
                executeMethod: () => ChangeInputRotate(false),
                canExecuteMethod: () => CanRotateRight());

            this.ClipAddCommand = new DelegateCommand<string>(
                executeMethod: n => AddSegments(n),
                canExecuteMethod: _ => inputService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.RemoveAsync(),
                canExecuteMethod: () => inputService.AreaService?.Area != default);
            this.ClipsRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.ClearAsync(),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Any() == true);
            this.ClipUndoSizeCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Undo(),
                canExecuteMethod: () => inputService.AreaService?.CanUndo == true);
            this.ClipsOrderAllCommand = new DelegateCommand(
                executeMethod: () => inputService.AreaService?.Order(),
                canExecuteMethod: () => inputService.AreaService?.Areas?.Any() == true);
            //this.ClipsEmptyCommand = new DelegateCommand(
            //    executeMethod: () => inputService.AreaService?.Empty(),
            //    canExecuteMethod: () => inputService.AreaService?.Areas?.Any() == true);

            this.TemplateSelectCommand = new DelegateCommand<Template>(
                executeMethod: t => SelectTemplate(t));
            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.TemplateService.RemoveAsync(),
                canExecuteMethod: () => inputService?.TemplateService?.Template != default);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService?.Create(inputService.AreaService.Segment),
                canExecuteMethod: () => inputService?.AreaService?.Segment != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.RemoveAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Sample != default);
            this.SamplesRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.ClearAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);
            this.SamplesOrderAllCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Order(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);

            tabSelectedEvent = eventAggregator.GetEvent<TabSelectedEvent>();
            detectionChangedEvent = eventAggregator.GetEvent<DetectionChangedEvent>();

            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: OnGraphicsUpdated);

            eventAggregator.GetEvent<InputsChangedEvent>().Subscribe(
                action: UpdateInputs);
            eventAggregator.GetEvent<VideoStartedEvent>().Subscribe(
                action: UpdateInputs);
            eventAggregator.GetEvent<VideoEndedEvent>().Subscribe(
                action: UpdateInputs);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: OnVideoUpdated);

            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: OnClipsChanged);
            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());
            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());

            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => OnClipsUpdated());

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: UpdateTemplates);
            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: UpdateSamples);
            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: _ => OnSampleSelected());

            eventAggregator.GetEvent<ScoreboardModifiedEvent>().Subscribe(
                action: () => ScoreboardUpdateCommand.RaiseCanExecuteChanged());
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxDuration => Constants.DurationMax;

        public static int MaxQueueSize => Constants.ImageQueueSizeMax;

        public static int MaxThreshold => Constants.ThresholdMax;

        public static int MinDelay => Constants.DelayMin;

        public static int MinQueueSize => Constants.ImageQueueSizeMin;

        public static string TabBoard => Constants.TabBoard;

        public static string TabSamples => Constants.TabSamples;

        public static string TabSegments => Constants.TabSegments;

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

        public DelegateCommand<string> ClipAddCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public DelegateCommand ClipsEmptyCommand { get; }

        public DelegateCommand ClipsOrderAllCommand { get; }

        public DelegateCommand ClipsRemoveAllCommand { get; }

        public DelegateCommand ClipUndoSizeCommand { get; }

        public DelegateCommand GraphicsReloadCommand { get; }

        public int ImagesQueueSize
        {
            get { return settingsService.Contents.Video.ImagesQueueSize; }
            set
            {
                if (IsActive
                    && value >= MinQueueSize
                    && value <= MaxQueueSize
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

        public ObservableCollection<RibbonDropDownItem> Inputs { get; } = new ObservableCollection<RibbonDropDownItem>();

        public DelegateCommand<Input> InputSelectCommand { get; }

        public DelegateCommand InputStopAllCommand { get; }

        public DelegateCommand InputsUpdateCommand { get; }

        public bool IsActive => inputService.IsActive;

        public bool IsDetectionAvailable => IsActive
            && inputService?.AreaService?.Segment != default;

        public bool IsSampleDetection
        {
            get
            {
                return inputService?.SampleService?.IsDetection ?? false;
            }
            set
            {
                if (IsDetectionAvailable
                    && inputService?.SampleService.IsDetection != value)
                {
                    inputService.SampleService.IsDetection = value;

                    detectionChangedEvent.Publish();

                    RaisePropertyChanged(nameof(IsSampleDetection));
                }
            }
        }

        public bool NoCropping
        {
            get { return settingsService.Contents.Video.NoCropping; }
            set
            {
                if (IsActive
                    && settingsService.Contents.Video.NoCropping != value)
                {
                    settingsService.Contents.Video.NoCropping = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(NoCropping));
                }
            }
        }

        public bool NoMultiComparison
        {
            get { return settingsService.Contents.Detection.NoMultiComparison; }
            set
            {
                if (IsActive
                    && settingsService.Contents.Detection.NoMultiComparison != value)
                {
                    settingsService.Contents.Detection.NoMultiComparison = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(NoMultiComparison));
                }
            }
        }

        public bool NoRecognition
        {
            get { return settingsService.Contents.Detection.NoRecognition; }
            set
            {
                if (IsActive
                    && settingsService.Contents.Detection.NoRecognition != value)
                {
                    settingsService.Contents.Detection.NoRecognition = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(NoRecognition));
                }
            }
        }

        public int ProcessingDelay
        {
            get { return settingsService.Contents.Video.ProcessingDelay; }
            set
            {
                if (IsActive
                    && value >= MinDelay
                    && value <= MaxDuration
                    && settingsService.Contents.Video.ProcessingDelay != value)
                {
                    settingsService.Contents.Video.ProcessingDelay = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ProcessingDelay));
                }
            }
        }

        public DelegateCommand SampleAddCommand { get; }

        public DelegateCommand SampleRemoveCommand { get; }

        public DelegateCommand SamplesOrderAllCommand { get; }

        public DelegateCommand SamplesRemoveAllCommand { get; }

        public DelegateCommand ScoreboardOpenCommand { get; }

        public DelegateCommand ScoreboardUpdateCommand { get; }

        public DelegateCommand<ViewType?> SelectTabCommand { get; }

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
                        case (int)ViewType.Board:

                            IsSampleDetection = false;

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Board));

                            tabSelectedEvent.Publish(ViewType.Board);

                            break;

                        case (int)ViewType.Areas:

                            IsSampleDetection = false;

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Areas));

                            tabSelectedEvent.Publish(ViewType.Areas);

                            break;

                        case (int)ViewType.Templates:

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Templates));

                            tabSelectedEvent.Publish(ViewType.Templates);

                            UpdateSamples();

                            break;
                    }
                }
            }
        }

        public DelegateCommand TemplateRemoveCommand { get; }

        public ObservableCollection<RibbonDropDownItem> Templates { get; } = new ObservableCollection<RibbonDropDownItem>();

        public DelegateCommand<Template> TemplateSelectCommand { get; }

        public int ThresholdDetecting
        {
            get { return settingsService.Contents.Detection.ThresholdDetecting; }
            set
            {
                if (IsActive
                    && value >= 0
                    && value <= MaxThreshold
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
                if (IsActive
                    && value >= 0
                    && value <= MaxThreshold
                    && settingsService.Contents.Detection.ThresholdMatching != value)
                {
                    settingsService.Contents.Detection.ThresholdMatching = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(ThresholdMatching));
                }
            }
        }

        public int WaitingDuration
        {
            get { return settingsService.Contents.Detection.WaitingDuration; }
            set
            {
                if (IsActive
                    && value >= 0
                    && value <= MaxDuration
                    && settingsService.Contents.Detection.WaitingDuration != value)
                {
                    settingsService.Contents.Detection.WaitingDuration = value;
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
                && settingsService.Contents.Video.Rotation >= Constants.RotateLeftMax;

            return result;
        }

        private bool CanRotateRight()
        {
            var result = inputService.IsActive
                && settingsService.Contents.Video.Rotation <= Constants.RotateRightMax;

            return result;
        }

        private void ChangeInputRotate(bool toLeft)
        {
            if (toLeft)
            {
                if (CanRotateLeft())
                {
                    settingsService.Contents.Video.Rotation -= Constants.RotateStep;
                    settingsService.Save();
                }
            }
            else
            {
                if (CanRotateRight())
                {
                    settingsService.Contents.Video.Rotation += Constants.RotateStep;
                    settingsService.Save();
                }
            }
        }

        private void OnClipsChanged()
        {
            ClipRemoveCommand.RaiseCanExecuteChanged();
            ClipsRemoveAllCommand.RaiseCanExecuteChanged();
            ClipsOrderAllCommand.RaiseCanExecuteChanged();

            SampleAddCommand.RaiseCanExecuteChanged();
        }

        private void OnClipsUpdated()
        {
            ClipUndoSizeCommand.RaiseCanExecuteChanged();
            ClipsOrderAllCommand.RaiseCanExecuteChanged();

            UpdateTemplates();
        }

        private void OnGraphicsUpdated()
        {
            GraphicsReloadCommand.RaiseCanExecuteChanged();

            ScoreboardOpenCommand.RaiseCanExecuteChanged();
            ScoreboardUpdateCommand.RaiseCanExecuteChanged();
        }

        private void OnSampleSelected()
        {
            SampleRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnTemplateSelected()
        {
            UpdateTemplates();

            TemplateRemoveCommand.RaiseCanExecuteChanged();
            SampleAddCommand.RaiseCanExecuteChanged();

            UpdateSamples();
        }

        private void OnVideoUpdated()
        {
            InputStopAllCommand.RaiseCanExecuteChanged();

            InputCenterCommand.RaiseCanExecuteChanged();
            InputRotateLeftCommand.RaiseCanExecuteChanged();
            InputRotateRightCommand.RaiseCanExecuteChanged();

            ClipAddCommand.RaiseCanExecuteChanged();
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
            var menuInputs = new HashSet<Guid>(Inputs.Where(i => i.CommandParameter != default).Select(i => (i.CommandParameter as Input).Guid));
            var serviceInputs = new HashSet<Guid>(inputService.Inputs.Select(i => i.Guid));

            if (!menuInputs.SetEquals(serviceInputs))
            {
                Inputs.Clear();

                var ordereds = inputService.Inputs
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

                var selectFileInput = new RibbonDropDownItem
                {
                    Command = InputSelectCommand,
                    Text = Texts.MenuInputFileText,
                };

                Inputs.Add(selectFileInput);

                RaisePropertyChanged(nameof(Inputs));
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(NoCropping));
                RaisePropertyChanged(nameof(ProcessingDelay));
                RaisePropertyChanged(nameof(ThresholdDetecting));
                RaisePropertyChanged(nameof(ThresholdMatching));
                RaisePropertyChanged(nameof(WaitingDuration));

                UpdateSamples();
            }
        }

        private void UpdateSamples()
        {
            RaisePropertyChanged(nameof(IsDetectionAvailable));

            SamplesRemoveAllCommand.RaiseCanExecuteChanged();
            SamplesOrderAllCommand.RaiseCanExecuteChanged();
        }

        private void UpdateTemplates()
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

        #endregion Private Methods
    }
}