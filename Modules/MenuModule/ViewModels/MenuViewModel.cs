using Core.Constants;
using Core.Enums;
using Core.Events;
using Core.Events.Input;
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

        private const int ViewIndexBoard = 0;
        private const int ViewIndexClip = 1;
        private const int ViewIndexTemplate = 2;

        private readonly IDialogService dialogService;
        private readonly IEventAggregator eventAggregator;
        private readonly IInputService inputService;
        private readonly IRegionManager regionManager;
        private readonly ITemplateService templateService;

        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(ITemplateService templateService, IGraphicsService graphicsService, IInputService inputService,
            IDialogService dialogService, IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.templateService = templateService;
            this.inputService = inputService;
            this.dialogService = dialogService;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            this.InputsUpdateCommand = new DelegateCommand(UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<Input>(i => SelectInput(i));

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Add(),
                canExecuteMethod: () => inputService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => RemoveClip(),
                canExecuteMethod: () => inputService.ClipService?.Active != default);
            this.ClipRemoveAllCommand = new DelegateCommand(
                executeMethod: () => RemoveAllClips(),
                canExecuteMethod: () => inputService.ClipService?.Clips?.Any() == true);

            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => UpdateInputs());
            eventAggregator.GetEvent<InputsChangedEvent>().Subscribe(
                action: UpdateInputs);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: OnVideoUpdated);

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: OnClipsChanged);
            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: OnTemplatesChanged);
            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: _ => OnSampleSelected());

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

            var addTemplateEvent = eventAggregator.GetEvent<SelectTemplateEvent>();
            this.ClipAsTemplateCommand = new DelegateCommand(
                executeMethod: () => addTemplateEvent.Publish(inputService.ClipService?.Active),
                canExecuteMethod: () => inputService.ClipService?.Active?.Bitmap != default);

            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: () => templateService.RemoveTemplate(),
                canExecuteMethod: () => templateService.Template != default);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => templateService.AddSample(),
                canExecuteMethod: () => templateService.Template != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => templateService.RemoveSample(),
                canExecuteMethod: () => templateService.Sample != default);

            inputService.Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipAsTemplateCommand { get; }

        public DelegateCommand ClipRemoveAllCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public DelegateCommand GraphicsEndCommand { get; }

        public DelegateCommand GraphicsOpenCommand { get; }

        public DelegateCommand GraphicsStartCommand { get; }

        public bool HasTemplates => templateService.Templates.Any();

        public ObservableCollection<Input> Inputs { get; } = new ObservableCollection<Input>();

        public DelegateCommand<Input> InputSelectCommand { get; }

        public DelegateCommand InputsUpdateCommand { get; }

        public DelegateCommand SampleAddCommand { get; }

        public DelegateCommand SampleRemoveCommand { get; }

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

        public ObservableCollection<TemplateViewModel> Templates { get; private set; } = new ObservableCollection<TemplateViewModel>();

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
            ClipRemoveAllCommand.RaiseCanExecuteChanged();

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

        private void OnTemplatesChanged()
        {
            Templates.Clear();

            var relevants = templateService.Templates
                .OrderBy(t => t.Clip.Name).ToArray();

            foreach (var relevant in relevants)
            {
                var current = new TemplateViewModel(
                    clip: relevant.Clip,
                    eventAggregator: eventAggregator);

                Templates.Add(current);
            }

            RaisePropertyChanged(nameof(HasTemplates));

            OnTemplateSelected();
        }

        private void OnTemplateSelected()
        {
            SelectedTabIndex = templateService.Template != default
                ? ViewIndexTemplate
                : ViewIndexClip;

            TemplateRemoveCommand.RaiseCanExecuteChanged();

            SampleAddCommand.RaiseCanExecuteChanged();
        }

        private void OnVideoUpdated()
        {
            ClipAddCommand.RaiseCanExecuteChanged();
            ClipAsTemplateCommand.RaiseCanExecuteChanged();
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
                inputService?.ClipService?.RemoveAll();
            }
        }

        private void RemoveClip()
        {
            var result = MessageBoxResult.Yes;

            if (inputService?.ClipService?.Active?.HasDimensions == true)
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

        private void SelectInput(Input input)
        {
            if (input.IsFile)
            {
                var fileName = input.FileName;

                if (!File.Exists(input.FileName))
                {
                    var settings = new OpenFileDialogSettings();
                    settings.Title = Constants.InputFileText;
                    settings.Multiselect = false;
                    settings.CheckFileExists = true;

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

        private void UpdateInputs()
        {
            Inputs.Clear();

            var ordereds = inputService.Inputs
                .OrderByDescending(i => i.Name != Constants.InputFileText)
                .ThenBy(i => i.IsFile)
                .ThenBy(i => i.Name).ToArray();

            foreach (var ordered in ordereds)
            {
                Inputs.Add(ordered);
            }

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

        #endregion Private Methods
    }
}