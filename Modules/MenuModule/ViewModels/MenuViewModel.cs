using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaUI.Ribbon;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Core.Constants;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Graphics;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Scoreboard;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models;
using Score2Stream.Core.Prism;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Score2Stream.MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int DurationMax = 1000;
        private const string TabNameBoard = "BoardTab";
        private const string TabNameTemplate = "TemplatesTab";
        private const string TabNameVideo = "VideoTab";
        private const int ThresholdMax = 100;

        private readonly IInputService inputService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IRegionManager regionManager;

        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IWebService webService, IScoreboardService scoreboardService, IInputService inputService,
            IMessageBoxService messageBoxService, IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.messageBoxService = messageBoxService;
            this.regionManager = regionManager;

            this.OnTabSelectionCommand = new DelegateCommand<string>(
                executeMethod: n => SelectRegion(n));

            this.InputsUpdateCommand = new DelegateCommand(
                executeMethod: UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<Input>(
                executeMethod: i => SelectInputAsync(i));
            this.InputStopAllCommand = new DelegateCommand(
                executeMethod: () => StopAllInputsAsync(),
                canExecuteMethod: () => inputService.IsActive);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Add(),
                canExecuteMethod: () => inputService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => RemoveClipAsync(),
                canExecuteMethod: () => inputService.ClipService?.Clip != default);
            this.ClipsRemoveAllCommand = new DelegateCommand(
                executeMethod: () => RemoveAllClipsAsync(),
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
                executeMethod: () => RemoveAllSamplesAsync(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);
            this.SamplesOrderCommand = new DelegateCommand(
                executeMethod: () => eventAggregator.GetEvent<OrderSamplesEvent>().Publish(),
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

            inputService.Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipAsTemplateCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public DelegateCommand ClipsRemoveAllCommand { get; }

        public DelegateCommand GraphicsOpenCommand { get; }

        public DelegateCommand GraphicsReloadCommand { get; }

        public bool HasTemplates => inputService?.TemplateService?.Templates?.Any() == true;

        public ObservableCollection<RibbonDropDownItem> Inputs { get; } = new ObservableCollection<RibbonDropDownItem>();

        public DelegateCommand<Input> InputSelectCommand { get; }

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
                }

                RaisePropertyChanged(nameof(IsSampleDetection));
            }
        }

        public bool NoCentering
        {
            get { return inputService.VideoService?.NoCentering ?? false; }
            set
            {
                if (IsActive)
                {
                    inputService.VideoService.NoCentering = value;
                }

                RaisePropertyChanged(nameof(NoCentering));
            }
        }

        public DelegateCommand<string> OnTabSelectionCommand { get; }

        public int ProcessingDelay
        {
            get { return inputService.VideoService?.Delay ?? 0; }
            set
            {
                if (IsActive
                    && value >= 0
                    && value <= DurationMax)
                {
                    inputService.VideoService.Delay = value;
                }

                RaisePropertyChanged(nameof(ProcessingDelay));
            }
        }

        public DelegateCommand SampleAddCommand { get; }

        public DelegateCommand SampleRemoveCommand { get; }

        public DelegateCommand SamplesOrderCommand { get; }

        public DelegateCommand SamplesRemoveAllCommand { get; }

        public DelegateCommand ScoreboardUpdateCommand { get; }

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                if (SelectedTabIndex != value)
                {
                    SetProperty(ref selectedTabIndex, value);

                    //UpdateRegions();
                }
            }
        }

        public DelegateCommand TemplateRemoveCommand { get; }

        public ObservableCollection<Template> Templates { get; } = new ObservableCollection<Template>();

        public DelegateCommand<Template> TemplateSelectCommand { get; }

        public int ThresholdDetecting
        {
            get { return inputService.VideoService?.ThresholdDetecting ?? 0; }
            set
            {
                if (IsActive
                    && value >= 0
                    && value <= ThresholdMax)
                {
                    inputService.VideoService.ThresholdDetecting = value;
                }

                RaisePropertyChanged(nameof(ThresholdDetecting));
            }
        }

        public int ThresholdMatching
        {
            get { return inputService.VideoService?.ThresholdMatching ?? 0; }
            set
            {
                if (IsActive
                    && value >= 0
                    && value <= ThresholdMax)
                {
                    inputService.VideoService.ThresholdMatching = value;
                }

                RaisePropertyChanged(nameof(ThresholdMatching));
            }
        }

        public int WaitingDuration
        {
            get { return inputService.VideoService?.WaitingDuration ?? 0; }
            set
            {
                if (IsActive
                    && value >= 0
                    && value <= DurationMax)
                {
                    inputService.VideoService.WaitingDuration = value;
                }

                RaisePropertyChanged(nameof(WaitingDuration));
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
            TemplateRemoveCommand.RaiseCanExecuteChanged();
            SampleAddCommand.RaiseCanExecuteChanged();

            var tabName = inputService.TemplateService?.Template != default
                ? TabNameTemplate
                : TabNameVideo;

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

        private async void RemoveAllClipsAsync()
        {
            var messageBoxParams = new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentMessage = "Shall all clips be removed?",
                ContentTitle = "Remove all clips",
                EnterDefaultButton = ClickEnum.Yes,
                EscDefaultButton = ClickEnum.No,
                Icon = Icon.Question,
                ShowInCenter = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var result = await messageBoxService.GetMessageBoxResultAsync(messageBoxParams);

            if (result == ButtonResult.Yes)
            {
                inputService?.ClipService?.Clear();
            }
        }

        private async void RemoveAllSamplesAsync()
        {
            var messageBoxParams = new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentMessage = "Shall all samples be removed?",
                ContentTitle = "Remove all samples",
                EnterDefaultButton = ClickEnum.Yes,
                EscDefaultButton = ClickEnum.No,
                Icon = Icon.Question,
                ShowInCenter = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var result = await messageBoxService.GetMessageBoxResultAsync(messageBoxParams);

            if (result == ButtonResult.Yes)
            {
                inputService?.SampleService?.Remove(inputService?.TemplateService?.Template);
            }
        }

        private async void RemoveClipAsync()
        {
            var result = ButtonResult.Yes;

            if (inputService?.ClipService?.Clip?.HasDimensions == true)
            {
                var messageBoxParams = new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentMessage = "Shall the selected clip be removed?",
                    ContentTitle = "Remove clip",
                    EnterDefaultButton = ClickEnum.Yes,
                    EscDefaultButton = ClickEnum.No,
                    Icon = Icon.Question,
                    ShowInCenter = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                result = await messageBoxService.GetMessageBoxResultAsync(messageBoxParams);
            }

            if (result == ButtonResult.Yes)
            {
                inputService?.ClipService?.Remove();
            }
        }

        private async void RemoveTemplateAsync()
        {
            var result = ButtonResult.Yes;

            if (inputService?.TemplateService?.Template != default)
            {
                var messageBoxParams = new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentMessage = "Shall the selected template be removed?",
                    ContentTitle = "Remove template",
                    EnterDefaultButton = ClickEnum.Yes,
                    EscDefaultButton = ClickEnum.No,
                    Icon = Icon.Question,
                    ShowInCenter = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                result = await messageBoxService.GetMessageBoxResultAsync(messageBoxParams);
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

        private async void SelectInputAsync(Input input)
        {
            if (input.IsFile)
            {
                var fileName = input.FileName;

                if (!File.Exists(input.FileName))
                {
                    var dialog = new OpenFileDialog
                    {
                        Title = Constants.InputFileText,
                        AllowMultiple = false
                    };

                    var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : default;

                    var result = await dialog.ShowAsync(mainWindow);

                    fileName = result?.FirstOrDefault();
                }

                if (File.Exists(fileName))
                {
                    inputService.Select(fileName);
                }
            }
            else
            {
                inputService.Select(input.DeviceId.Value);
            }
        }

        private void SelectRegion(string name)
        {
            switch (name)
            {
                case TabNameBoard:

                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Board));
                    break;

                case TabNameVideo:

                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Clips));

                    break;

                case TabNameTemplate:

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
            var messageBoxParams = new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentMessage = "Shall all inputs be stopped?",
                ContentTitle = "Stop inputs",
                EnterDefaultButton = ClickEnum.Yes,
                EscDefaultButton = ClickEnum.No,
                Icon = Icon.Question,
                ShowInCenter = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var result = await messageBoxService.GetMessageBoxResultAsync(messageBoxParams);

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

                Templates.AddRange(ordereds);

                RaisePropertyChanged(nameof(Templates));
                RaisePropertyChanged(nameof(HasTemplates));
            }
        }

        #endregion Private Methods
    }
}