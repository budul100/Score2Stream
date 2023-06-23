using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaUI.Ribbon;
using MessageBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Core.Constants;
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
using Score2Stream.Core.Models;
using Score2Stream.Core.Prism;
using Score2Stream.Core.Settings;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Score2Stream.MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int TabBoardIndex = 0;
        private const string TabBoardName = "BoardTab";
        private const int TabSamplesIndex = 2;
        private const string TabSamplesName = "SamplesTab";
        private const int TabVideoIndex = 1;
        private const string TabVideoName = "VideoTab";

        private readonly IEventAggregator eventAggregator;
        private readonly IInputService inputService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IRegionManager regionManager;
        private readonly UserSettings settings;
        private readonly ISettingsService<UserSettings> settingsService;
        private string inputDirectory;
        private int tabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(ISettingsService<UserSettings> settingsService, IWebService webService,
            IScoreboardService scoreboardService, IInputService inputService, IMessageBoxService messageBoxService,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.settingsService = settingsService;
            this.inputService = inputService;
            this.messageBoxService = messageBoxService;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            this.settings = settingsService.Get();

            this.OnTabSelectionCommand = new DelegateCommand<string>(
                executeMethod: n => SelectRegion(n));

            this.InputsUpdateCommand = new DelegateCommand(
                executeMethod: UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<Core.Models.Input>(
                executeMethod: i => SelectInputAsync(i));
            this.InputStopAllCommand = new DelegateCommand(
                executeMethod: () => StopAllInputsAsync(),
                canExecuteMethod: () => inputService.IsActive);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Add(),
                canExecuteMethod: () => inputService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => RemoveClipSelectedAsync(),
                canExecuteMethod: () => inputService.ClipService?.Clip != default);
            this.ClipsRemoveAllCommand = new DelegateCommand(
                executeMethod: () => RemoveClipsAllAsync(),
                canExecuteMethod: () => inputService.ClipService?.Clips?.Any() == true);

            this.ClipAsTemplateCommand = new DelegateCommand(
                executeMethod: () => inputService.TemplateService.Add(inputService.ClipService.Clip),
                canExecuteMethod: () => inputService.ClipService?.Clip != default);

            this.TemplateSelectCommand = new DelegateCommand<Template>(
                executeMethod: t => SelectTemplate(t));
            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: () => RemoveTemplateAsync(),
                canExecuteMethod: () => inputService?.TemplateService?.Template != default);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Add(inputService.ClipService.Clip),
                canExecuteMethod: () => inputService?.ClipService?.Clip != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Remove(),
                canExecuteMethod: () => inputService?.SampleService?.Sample != default);
            this.SamplesRemoveAllCommand = new DelegateCommand(
                executeMethod: () => RemoveSamplesAllAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);
            this.SamplesOrderCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Order(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);

            this.GraphicsReloadCommand = new DelegateCommand(
                executeMethod: async () => await webService.ReloadAsync());
            this.GraphicsOpenCommand = new DelegateCommand(
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
                action: _ => UpdateTemplates());

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
            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => ScoreboardUpdateCommand.RaiseCanExecuteChanged());

            inputService.Initialize();
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxDuration => Constants.MaxDuration;

        public static int MaxQueueSize => Constants.MaxQueueSize;

        public static int MaxThreshold => Constants.MaxThreshold;

        public static int MinQueueSize => Constants.MinQueueSize;

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipAsTemplateCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public DelegateCommand ClipsRemoveAllCommand { get; }

        public DelegateCommand GraphicsOpenCommand { get; }

        public DelegateCommand GraphicsReloadCommand { get; }

        public bool HasTemplates => inputService?.TemplateService?.Templates?.Any() == true;

        public int ImagesQueueSize
        {
            get { return inputService.ImagesQueueSize; }
            set
            {
                if (inputService.ImagesQueueSize != value
                    && value >= MinQueueSize
                    && value <= MaxQueueSize)
                {
                    inputService.ImagesQueueSize = value;

                    RaisePropertyChanged(nameof(ImagesQueueSize));
                }
            }
        }

        public ObservableCollection<RibbonDropDownItem> Inputs { get; } = new ObservableCollection<RibbonDropDownItem>();

        public DelegateCommand<Core.Models.Input> InputSelectCommand { get; }

        public DelegateCommand InputStopAllCommand { get; }

        public DelegateCommand InputsUpdateCommand { get; }

        public bool IsActive => inputService.IsActive;

        public bool IsSampleDetection
        {
            get { return inputService?.SampleService?.IsDetection ?? false; }
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

        public bool NoCentering
        {
            get { return inputService.NoCentering; }
            set
            {
                if (inputService.NoCentering != value)
                {
                    inputService.NoCentering = value;

                    RaisePropertyChanged(nameof(NoCentering));
                }
            }
        }

        public DelegateCommand<string> OnTabSelectionCommand { get; }

        public int ProcessingDelay
        {
            get { return inputService.ProcessingDelay; }
            set
            {
                if (inputService.ProcessingDelay != value
                    && value >= 0
                    && value <= MaxDuration)
                {
                    inputService.ProcessingDelay = value;

                    RaisePropertyChanged(nameof(ProcessingDelay));
                }
            }
        }

        public DelegateCommand SampleAddCommand { get; }

        public DelegateCommand SampleRemoveCommand { get; }

        public DelegateCommand SamplesOrderCommand { get; }

        public DelegateCommand SamplesRemoveAllCommand { get; }

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
            get { return inputService.ThresholdDetecting; }
            set
            {
                if (inputService.ThresholdDetecting != value
                    && value >= 0
                    && value <= MaxThreshold)
                {
                    inputService.ThresholdDetecting = value;

                    RaisePropertyChanged(nameof(ThresholdDetecting));
                }
            }
        }

        public int ThresholdMatching
        {
            get { return inputService.ThresholdMatching; }
            set
            {
                if (inputService.ThresholdMatching != value
                    && value >= 0
                    && value <= MaxThreshold)
                {
                    inputService.ThresholdMatching = value;

                    RaisePropertyChanged(nameof(ThresholdMatching));
                }
            }
        }

        public int WaitingDuration
        {
            get { return inputService.WaitingDuration; }
            set
            {
                if (inputService.WaitingDuration != value
                    && value >= 0
                    && value <= MaxDuration)
                {
                    inputService.WaitingDuration = value;

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

        private void OnClipsChanged()
        {
            ClipAddCommand.RaiseCanExecuteChanged();
            ClipRemoveCommand.RaiseCanExecuteChanged();
            ClipsRemoveAllCommand.RaiseCanExecuteChanged();

            ClipAsTemplateCommand.RaiseCanExecuteChanged();
        }

        private void OnGraphicsUpdated()
        {
            GraphicsReloadCommand.RaiseCanExecuteChanged();
            GraphicsOpenCommand.RaiseCanExecuteChanged();

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

            var tabName = inputService.TemplateService?.Template != default
                ? TabSamplesName
                : TabVideoName;

            SelectRegion(tabName);
        }

        private void OnVideoUpdated()
        {
            InputStopAllCommand.RaiseCanExecuteChanged();

            ClipAddCommand.RaiseCanExecuteChanged();
            ClipAsTemplateCommand.RaiseCanExecuteChanged();

            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(NoCentering));
            RaisePropertyChanged(nameof(ProcessingDelay));
            RaisePropertyChanged(nameof(ThresholdDetecting));
            RaisePropertyChanged(nameof(ThresholdMatching));
            RaisePropertyChanged(nameof(WaitingDuration));
        }

        private async void RemoveClipsAllAsync()
        {
            var result = await messageBoxService.GetMessageBoxResultAsync(
                contentMessage: "Shall all clips be removed?",
                contentTitle: "Remove all clips");

            if (result == ButtonResult.Yes)
            {
                inputService?.ClipService?.Clear();
            }
        }

        private async void RemoveClipSelectedAsync()
        {
            var result = ButtonResult.Yes;

            if (inputService?.ClipService?.Clip?.HasDimensions == true)
            {
                result = await messageBoxService.GetMessageBoxResultAsync(
                    contentMessage: "Shall the selected clip be removed?",
                    contentTitle: "Remove clip");
            }

            if (result == ButtonResult.Yes)
            {
                inputService?.ClipService?.Remove();
            }
        }

        private async void RemoveSamplesAllAsync()
        {
            var result = await messageBoxService.GetMessageBoxResultAsync(
                contentMessage: "Shall all samples be removed?",
                contentTitle: "Remove all samples");

            if (result == ButtonResult.Yes)
            {
                inputService?.SampleService?.Remove(inputService?.TemplateService?.Template);
            }
        }

        private async void RemoveTemplateAsync()
        {
            var result = ButtonResult.Yes;

            if (inputService?.TemplateService?.Template != default)
            {
                result = await messageBoxService.GetMessageBoxResultAsync(
                    contentMessage: "Shall the selected template be removed?",
                    contentTitle: "Remove template");
            }

            if (result == ButtonResult.Yes
                && inputService?.TemplateService?.Template != default)
            {
                inputService.TemplateService.Remove();

                var template = inputService.ClipService?.Clip?.Template
                    ?? inputService.TemplateService.Templates.FirstOrDefault();
                inputService.TemplateService.Select(template);
            }
        }

        private void SelectInput(Core.Models.Input input)
        {
            if (!input.IsFile)
            {
                inputService.Select(
                    deviceId: input.DeviceId.Value);
            }
            else if (File.Exists(input.FileName))
            {
                inputService.Select(
                    fileName: input.FileName);
            }
        }

        private async void SelectInputAsync(Core.Models.Input input)
        {
            if (input.IsFile
                && !File.Exists(input.FileName))
            {
                if (!Directory.Exists(inputDirectory))
                {
                    inputDirectory = Path.GetDirectoryName(settings.Video.FilePathVideo);
                }

                var dialog = new OpenFileDialog
                {
                    Directory = inputDirectory,
                    Title = Constants.InputFileText,
                    AllowMultiple = false
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : default;

                var result = await dialog.ShowAsync(mainWindow);

                input.FileName = result?.FirstOrDefault();

                if (File.Exists(input.FileName))
                {
                    settings.Video.FilePathVideo = input.FileName;
                    settingsService.Save();
                }
            }

            SelectInput(input);
        }

        private void SelectRegion(string name)
        {
            switch (name)
            {
                case TabBoardName:

                    TabIndex = TabBoardIndex;
                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Board));
                    break;

                case TabVideoName:

                    TabIndex = TabVideoIndex;
                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Clips));

                    break;

                case TabSamplesName:

                    TabIndex = TabSamplesIndex;
                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Templates));
                    break;
            }
        }

        private void SelectTemplate(Template template)
        {
            if (template != default)
            {
                inputService?.TemplateService?.Select(template);
                inputService?.ClipService?.Select(template.Clip);
            }
        }

        private async void StopAllInputsAsync()
        {
            var result = await messageBoxService.GetMessageBoxResultAsync(
                contentMessage: "Shall all inputs be stopped?",
                contentTitle: "Stop inputs");

            if (result == ButtonResult.Yes)
            {
                inputService?.VideoService?.StopAll();
            }
        }

        private void UpdateInputs()
        {
            Inputs.Clear();

            var ordereds = inputService.Inputs
                .OrderByDescending(i => i.Name != Constants.InputFileText)
                .ThenBy(i => i.IsFile)
                .ThenBy(i => i.Name).ToArray();

            foreach (var ordered in ordereds)
            {
                var input = new RibbonDropDownItem
                {
                    Command = InputSelectCommand,
                    CommandParameter = ordered,
                    IsChecked = ordered.IsActive,
                    Text = ordered.Name
                };

                Inputs.Add(input);
            }

            RaisePropertyChanged(nameof(Inputs));
        }

        private void UpdateSamples()
        {
            SamplesRemoveAllCommand.RaiseCanExecuteChanged();
            SamplesOrderCommand.RaiseCanExecuteChanged();
        }

        private void UpdateTemplates()
        {
            Templates.Clear();

            if (inputService.TemplateService != default)
            {
                var ordereds = inputService.TemplateService.Templates
                    .OrderBy(t => t.Description).ToArray();

                foreach (var ordered in ordereds)
                {
                    var isChecked = ordered == inputService.TemplateService.Template;

                    var template = new RibbonDropDownItem
                    {
                        Command = TemplateSelectCommand,
                        CommandParameter = ordered,
                        IsChecked = isChecked,
                        Text = ordered.Description,
                    };

                    Templates.Add(template);
                }

                RaisePropertyChanged(nameof(Templates));
                RaisePropertyChanged(nameof(HasTemplates));
            }
        }

        #endregion Private Methods
    }
}