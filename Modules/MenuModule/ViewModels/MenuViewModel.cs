using Core.Constants;
using Core.Enums;
using Core.Events;
using Core.Events.Input;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using Core.Prism;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Linq;

namespace MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int ViewIndexBoard = 0;
        private const int ViewIndexClip = 1;
        private const int ViewIndexTemplate = 2;

        private readonly IEventAggregator eventAggregator;
        private readonly IInputService inputService;
        private readonly IRegionManager regionManager;
        private readonly ITemplateService templateService;

        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(ITemplateService templateService, IGraphicsService graphicsService, IInputService inputService,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.templateService = templateService;
            this.inputService = inputService;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            this.InputsUpdateCommand = new DelegateCommand(UpdateInputs);
            this.InputSelectCommand = new DelegateCommand<Input>(i => inputService.Select(i));

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
                executeMethod: async () => await graphicsService.StartAsync(GraphicsUrls.PortHttpWebServer, GraphicsUrls.PortHttpWebSocket),
                canExecuteMethod: () => !graphicsService.IsActive);
            this.GraphicsEndCommand = new DelegateCommand(
                executeMethod: async () => await graphicsService.StopAsync(),
                canExecuteMethod: () => graphicsService.IsActive);
            this.GraphicsOpenCommand = new DelegateCommand(
                executeMethod: () => graphicsService.Open(),
                canExecuteMethod: () => graphicsService.IsActive);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Add(),
                canExecuteMethod: () => inputService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => inputService.ClipService?.Remove(),
                canExecuteMethod: () => inputService.ClipService?.Clip != default);

            var addTemplateEvent = eventAggregator.GetEvent<SelectTemplateEvent>();
            this.ClipAsTemplateCommand = new DelegateCommand(
                executeMethod: () => addTemplateEvent.Publish(inputService.ClipService?.Clip),
                canExecuteMethod: () => inputService.ClipService?.Clip?.Bitmap != default);

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

        private void UpdateInputs()
        {
            Inputs.Clear();
            Inputs.AddRange(inputService.Inputs);

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