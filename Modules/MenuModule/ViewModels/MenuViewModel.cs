using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AvaloniaUI.Ribbon;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Core;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Detection;
using Score2Stream.Core.Events.Graphics;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Scoreboard;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using Score2Stream.Core.Models.Settings;
using Score2Stream.Core.Prism;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Score2Stream.MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int IndexBoard = 0;
        private const int IndexClips = 1;
        private const int IndexSamples = 2;
        private const string InputFileText = "Select file ...";
        private const string TemplateAddText = "Add new template";

        private readonly IEventAggregator eventAggregator;
        private readonly IInputService inputService;
        private readonly IRegionManager regionManager;
        private readonly Session settings;
        private readonly ISettingsService<Session> settingsService;

        private IStorageFolder inputDirectory;
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
            this.eventAggregator = eventAggregator;

            this.settings = settingsService.Get();

            this.OnTabSelectionCommand = new DelegateCommand<string>(
                executeMethod: n => SelectTab(n));

            this.InputsUpdateCommand = new DelegateCommand(
                executeMethod: UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<Input>(
                executeMethod: i => SelectInputAsync(i));
            this.InputStopAllCommand = new DelegateCommand(
                executeMethod: () => inputService.StopAsync(),
                canExecuteMethod: () => inputService.IsActive);

            this.InputCenterCommand = new DelegateCommand(
                executeMethod: ChangeInputCentred,
                canExecuteMethod: () => inputService.IsActive);
            this.InputRotateLeftCommand = new DelegateCommand(
                executeMethod: ChangeInputRotateLeft,
                canExecuteMethod: () => inputService.VideoService?.RotationLeftPossible == true);
            this.InputRotateRightCommand = new DelegateCommand(
                executeMethod: ChangeInputRotateRight,
                canExecuteMethod: () => inputService.VideoService?.RotationRightPossible == true);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Create(),
                canExecuteMethod: () => inputService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.RemoveAsync(),
                canExecuteMethod: () => inputService.ClipService?.Active != default);
            this.ClipsRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.ClearAsync(),
                canExecuteMethod: () => inputService.ClipService?.Clips?.Any() == true);
            this.ClipsOrderAllCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Order(),
                canExecuteMethod: () => inputService.ClipService?.Clips?.Any() == true);
            this.ClipUndoSizeCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.UndoSize(),
                canExecuteMethod: () => inputService.ClipService?.UndoSizePossible == true);

            this.TemplateSelectCommand = new DelegateCommand<Template>(
                executeMethod: t => SelectTemplate(t));
            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.TemplateService.RemoveAsync(),
                canExecuteMethod: () => inputService?.TemplateService?.Active != default);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService?.Create(inputService.ClipService.Active),
                canExecuteMethod: () => inputService?.ClipService?.Active != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.RemoveAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Active != default);
            this.SamplesRemoveAllCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.ClearAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);
            this.SamplesOrderAllCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Order(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);

            this.GraphicsReloadCommand = new DelegateCommand(
                executeMethod: async () => await webService.ReloadAsync());
            this.ScoreboardOpenCommand = new DelegateCommand(
                executeMethod: () => webService.Open(),
                canExecuteMethod: () => webService.IsActive);

            this.ScoreboardUpdateCommand = new DelegateCommand(
                executeMethod: () => scoreboardService.Update(),
                canExecuteMethod: () => !scoreboardService.UpToDate);

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

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: OnClipsChanged);
            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());
            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => OnClipsUpdated());

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: UpdateTemplates);
            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: UpdateSamples);
            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: _ => OnSampleSelected());

            eventAggregator.GetEvent<ScoreboardChangedEvent>().Subscribe(
                action: () => ScoreboardUpdateCommand.RaiseCanExecuteChanged());

            inputService.Initialize();
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxDuration => Constants.DurationMax;

        public static int MaxQueueSize => Constants.ImageQueueSizeMax;

        public static int MaxThreshold => Constants.ThresholdMax;

        public static int MinDelay => Constants.DelayMin;

        public static int MinQueueSize => Constants.ImageQueueSizeMin;

        public static string TabBoard => Constants.TabBoard;

        public static string TabClips => Constants.TabClips;

        public static string TabSamples => Constants.TabSamples;

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public DelegateCommand ClipsOrderAllCommand { get; }

        public DelegateCommand ClipsRemoveAllCommand { get; }

        public DelegateCommand ClipUndoSizeCommand { get; }

        public DelegateCommand GraphicsReloadCommand { get; }

        public int ImagesQueueSize
        {
            get
            {
                return inputService?.VideoService?.ImagesQueueSize
                    ?? Constants.ImageQueueSizeDefault;
            }
            set
            {
                if (IsActive
                    && inputService.VideoService.ImagesQueueSize != value
                    && value >= MinQueueSize
                    && value <= MaxQueueSize)
                {
                    inputService.VideoService.ImagesQueueSize = value;
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

        public bool IsSampleDetection
        {
            get
            {
                return inputService?.SampleService?.IsDetection ?? false;
            }
            set
            {
                if (IsActive)
                {
                    inputService.SampleService.IsDetection = value;

                    eventAggregator.GetEvent<DetectionChangedEvent>().Publish();
                    RaisePropertyChanged(nameof(IsSampleDetection));
                }
            }
        }

        public bool NoCropping
        {
            get { return inputService?.VideoService?.NoCropping ?? false; }
            set
            {
                if (IsActive
                    && inputService.VideoService.NoCropping != value)
                {
                    inputService.VideoService.NoCropping = value;
                    RaisePropertyChanged(nameof(NoCropping));
                }
            }
        }

        public bool NoRecognition
        {
            get { return inputService?.SampleService?.NoRecognition ?? false; }
            set
            {
                if (IsActive
                    && inputService.SampleService.NoRecognition != value)
                {
                    inputService.SampleService.NoRecognition = value;
                    RaisePropertyChanged(nameof(NoRecognition));
                }
            }
        }

        public DelegateCommand<string> OnTabSelectionCommand { get; }

        public int ProcessingDelay
        {
            get { return inputService?.VideoService?.ProcessingDelay ?? MinDelay; }
            set
            {
                if (IsActive
                    && inputService.VideoService.ProcessingDelay != value
                    && value >= MinDelay
                    && value <= MaxDuration)
                {
                    inputService.VideoService.ProcessingDelay = value;
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

        public int TabIndex
        {
            get { return tabIndex; }
            set
            {
                if (TabIndex != value)
                {
                    SetProperty(ref tabIndex, value);
                }
            }
        }

        public DelegateCommand TemplateRemoveCommand { get; }

        public ObservableCollection<RibbonDropDownItem> Templates { get; } = new ObservableCollection<RibbonDropDownItem>();

        public DelegateCommand<Template> TemplateSelectCommand { get; }

        public int ThresholdDetecting
        {
            get { return inputService?.SampleService?.ThresholdDetecting ?? 0; }
            set
            {
                if (IsActive
                    && inputService.SampleService.ThresholdDetecting != value
                    && value >= 0
                    && value <= MaxThreshold)
                {
                    inputService.SampleService.ThresholdDetecting = value;
                    RaisePropertyChanged(nameof(ThresholdDetecting));
                }
            }
        }

        public int ThresholdMatching
        {
            get { return inputService?.VideoService?.ThresholdMatching ?? 0; }
            set
            {
                if (IsActive
                    && inputService.VideoService.ThresholdMatching != value
                    && value >= 0
                    && value <= MaxThreshold)
                {
                    inputService.VideoService.ThresholdMatching = value;
                    RaisePropertyChanged(nameof(ThresholdMatching));
                }
            }
        }

        public int WaitingDuration
        {
            get { return inputService?.VideoService?.WaitingDuration ?? 0; }
            set
            {
                if (IsActive
                    && inputService.VideoService.WaitingDuration != value
                    && value >= 0
                    && value <= MaxDuration)
                {
                    inputService.VideoService.WaitingDuration = value;
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

        private void ChangeInputCentred()
        {
            eventAggregator.GetEvent<VideoCenteredEvent>().Publish();
        }

        private void ChangeInputRotateLeft()
        {
            if (inputService?.VideoService?.RotationLeftPossible == true)
            {
                inputService.VideoService.Rotation -= Constants.RotateStep;
            }
        }

        private void ChangeInputRotateRight()
        {
            if (inputService?.VideoService?.RotationRightPossible == true)
            {
                inputService.VideoService.Rotation += Constants.RotateStep;
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

        private async void SelectInputAsync(Input input)
        {
            if (input?.IsDevice == true)
            {
                inputService.Select(input);
            }
            else
            {
                var path = input?.FileName;

                if (input?.IsActive != true || !File.Exists(path))
                {
                    var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : default;

                    var topLevel = Window.GetTopLevel(mainWindow);

                    if (!Directory.Exists(inputDirectory?.Path?.AbsolutePath)
                        && Directory.Exists(Path.GetDirectoryName(settings.Video.FilePathVideo)))
                    {
                        var folderPath = Path.GetDirectoryName(settings.Video.FilePathVideo);
                        inputDirectory = await topLevel.StorageProvider.TryGetFolderFromPathAsync(folderPath);
                    }

                    var paths = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        SuggestedStartLocation = inputDirectory,
                        Title = InputFileText,
                        AllowMultiple = false
                    });

                    path = paths.Count >= 1
                        ? paths[0].Path.AbsolutePath
                        : default;
                }

                if (File.Exists(path))
                {
                    settings.Video.FilePathVideo = path;
                    settingsService.Save();
                }

                inputService.Select(
                    fileName: path);
            }
        }

        private void SelectTab(string tabName)
        {
            switch (tabName)
            {
                case Constants.TabBoard:

                    IsSampleDetection = false;

                    TabIndex = IndexBoard;
                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Board));
                    break;

                case Constants.TabClips:

                    IsSampleDetection = false;

                    TabIndex = IndexClips;
                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Clips));

                    break;

                case Constants.TabSamples:

                    if (inputService?.IsActive == true
                        && inputService?.ClipService?.Clips?.Any() == true
                        && inputService?.ClipService?.Active == default)
                    {
                        inputService.ClipService.Select(inputService.ClipService.Clips[0]);
                    }

                    TabIndex = IndexSamples;
                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Templates));
                    break;
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
                    Text = InputFileText,
                };

                Inputs.Add(selectFileInput);

                RaisePropertyChanged(nameof(Inputs));
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(NoCropping));
                RaisePropertyChanged(nameof(ProcessingDelay));
                RaisePropertyChanged(nameof(ThresholdDetecting));
                RaisePropertyChanged(nameof(ThresholdMatching));
                RaisePropertyChanged(nameof(WaitingDuration));
            }
        }

        private void UpdateSamples()
        {
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
                    var isChecked = ordered == inputService.TemplateService.Active;

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
                    Text = TemplateAddText,
                };

                Templates.Add(selectTemplateAdd);

                RaisePropertyChanged(nameof(Templates));
            }
        }

        #endregion Private Methods
    }
}