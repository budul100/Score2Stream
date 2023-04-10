using Core.Constants;
using Core.Enums;
using Core.Events;
using Core.Events.Clip;
using Core.Events.Input;
using Core.Events.Sample;
using Core.Events.Template;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using Core.Prism;
using MvvmDialogs;
using MvvmDialogs.FrameworkDialogs.OpenFile;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int DurationMax = 1000;
        private const int ThresholdMax = 100;
        private const int ViewIndexBoard = 0;
        private const int ViewIndexClip = 1;
        private const int ViewIndexTemplate = 2;

        private readonly IDialogService dialogService;
        private readonly IInputService inputService;
        private readonly IRegionManager regionManager;

        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IGraphicsService graphicsService, IInputService inputService, IDialogService dialogService,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.dialogService = dialogService;
            this.regionManager = regionManager;

            this.InputsUpdateCommand = new DelegateCommand(
                executeMethod: UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<Input>(
                executeMethod: i => SelectInput(i));
            this.InputStopAllCommand = new DelegateCommand(
                executeMethod: () => StopAllInputs(),
                canExecuteMethod: () => inputService.IsActive);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Add(),
                canExecuteMethod: () => inputService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => RemoveClip(),
                canExecuteMethod: () => inputService.ClipService?.Clip != default);
            this.ClipsRemoveAllCommand = new DelegateCommand(
                executeMethod: () => RemoveAllClips(),
                canExecuteMethod: () => inputService.ClipService?.Clips?.Any() == true);

            this.ClipAsTemplateCommand = new DelegateCommand(
                executeMethod: () => inputService.TemplateService.Add(inputService.ClipService.Clip),
                canExecuteMethod: () => inputService.ClipService?.Clip != default);

            this.TemplateSelectCommand = new DelegateCommand<Template>(
                executeMethod: t => SelectTemplate(t));
            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: () => RemoveTemplate(),
                canExecuteMethod: () => inputService?.TemplateService?.Template != default);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Add(inputService.ClipService.Clip),
                canExecuteMethod: () => inputService?.ClipService?.Clip != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.SampleService.Remove(),
                canExecuteMethod: () => inputService?.SampleService?.Sample != default);
            this.SamplesRemoveAllCommand = new DelegateCommand(
                executeMethod: () => RemoveAllSamples(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);
            this.SamplesOrderCommand = new DelegateCommand(
                executeMethod: () => eventAggregator.GetEvent<OrderSamplesEvent>().Publish(),
                canExecuteMethod: () => inputService?.SampleService?.Samples?.Any() == true);

            eventAggregator.GetEvent<InputsChangedEvent>().Subscribe(
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

            /// Must be tidied
            eventAggregator.GetEvent<GraphicsUpdatedEvent>().Subscribe(
                action: OnGraphicsUpdated);

            this.GraphicsStartCommand = new DelegateCommand(
                executeMethod: async () => await graphicsService.StartAsync(Constants.PortHttpWebServer, Constants.PortHttpWebSocket),
                canExecuteMethod: () => !graphicsService.IsActive);
            this.GraphicsEndCommand = new DelegateCommand(
                executeMethod: async () => await graphicsService.StopAsync(),
                canExecuteMethod: () => graphicsService.IsActive);
            this.GraphicsOpenCommand = new DelegateCommand(
                executeMethod: () => graphicsService.Open(),
                canExecuteMethod: () => graphicsService.IsActive);

            inputService.Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipAsTemplateCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public DelegateCommand ClipsRemoveAllCommand { get; }

        public DelegateCommand GraphicsEndCommand { get; }

        public DelegateCommand GraphicsOpenCommand { get; }

        public DelegateCommand GraphicsStartCommand { get; }

        public bool HasTemplates => inputService?.TemplateService?.Templates?.Any() == true;

        public ObservableCollection<Input> Inputs { get; } = new ObservableCollection<Input>();

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

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                if (SelectedTabIndex != value)
                {
                    SetProperty(ref selectedTabIndex, value);

                    UpdateRegions();
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
            GraphicsStartCommand.RaiseCanExecuteChanged();
            GraphicsEndCommand.RaiseCanExecuteChanged();
            GraphicsOpenCommand.RaiseCanExecuteChanged();
        }

        private void OnSampleSelected()
        {
            SampleRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnTemplateSelected()
        {
            SelectedTabIndex = inputService.TemplateService?.Template != default
                ? ViewIndexTemplate
                : ViewIndexClip;

            TemplateRemoveCommand.RaiseCanExecuteChanged();
            SampleAddCommand.RaiseCanExecuteChanged();
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

        private void RemoveAllClips()
        {
            var result = dialogService.ShowMessageBox(
                ownerViewModel: this,
                messageBoxText: "Shall all clips be removed?",
                caption: "Remove all clips",
                button: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Question,
                defaultResult: MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                inputService?.ClipService?.Clear();
            }
        }

        private void RemoveAllSamples()
        {
            var result = dialogService.ShowMessageBox(
                ownerViewModel: this,
                messageBoxText: "Shall all samples be removed?",
                caption: "Remove all samples",
                button: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Question,
                defaultResult: MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                inputService?.SampleService?.Remove(inputService?.TemplateService?.Template);
            }
        }

        private void RemoveClip()
        {
            var result = MessageBoxResult.Yes;

            if (inputService?.ClipService?.Clip?.HasDimensions == true)
            {
                result = dialogService.ShowMessageBox(
                    ownerViewModel: this,
                    messageBoxText: "Shall the current clip be removed?",
                    caption: "Remove clip",
                    button: MessageBoxButton.YesNo,
                    icon: MessageBoxImage.Question,
                    defaultResult: MessageBoxResult.No);
            }

            if (result == MessageBoxResult.Yes)
            {
                inputService?.ClipService?.Remove();
            }
        }

        private void RemoveTemplate()
        {
            var result = MessageBoxResult.Yes;

            if (inputService?.TemplateService?.Template != default)
            {
                result = dialogService.ShowMessageBox(
                    ownerViewModel: this,
                    messageBoxText: "Shall the current template be removed?",
                    caption: "Remove template",
                    button: MessageBoxButton.YesNo,
                    icon: MessageBoxImage.Question,
                    defaultResult: MessageBoxResult.No);
            }

            if (result == MessageBoxResult.Yes
                && inputService?.TemplateService?.Template != default)
            {
                inputService.TemplateService.Remove();

                var template = inputService.ClipService?.Clip?.Template
                    ?? inputService.TemplateService.Templates.FirstOrDefault();
                inputService.TemplateService.Select(template);
            }
        }

        private void SelectInput(Input input)
        {
            if (input.IsFile)
            {
                var fileName = input.FileName;

                if (!File.Exists(input.FileName))
                {
                    var settings = new OpenFileDialogSettings
                    {
                        Title = Constants.InputFileText,
                        Multiselect = false,
                        CheckFileExists = true
                    };

                    var result = dialogService.ShowOpenFileDialog(
                        ownerViewModel: this,
                        settings: settings);

                    if (result ?? false)
                    {
                        fileName = settings.FileName;
                    }
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

        private void SelectTemplate(Template template)
        {
            if (template != default)
            {
                inputService?.TemplateService?.Select(template);
                inputService?.ClipService?.Select(template.Clip);
            }
        }

        private void StopAllInputs()
        {
            var result = dialogService.ShowMessageBox(
                ownerViewModel: this,
                messageBoxText: "Shall all inputs be stopped?",
                caption: "Stop all inputs",
                button: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Question,
                defaultResult: MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
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

            Inputs.AddRange(ordereds);

            RaisePropertyChanged(nameof(Inputs));
        }

        private void UpdateRegions()
        {
            switch (SelectedTabIndex)
            {
                case ViewIndexBoard:

                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Board));
                    break;

                case ViewIndexClip:
                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Clips));

                    break;

                case ViewIndexTemplate:

                    regionManager.RequestNavigate(
                        regionName: nameof(RegionType.EditRegion),
                        source: nameof(ViewType.Templates));
                    break;
            }
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