using Core.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using ScoreboardOCR.Core.Constants;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;

namespace MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int IndexClipView = 1;
        private const int IndexTemplateView = 2;

        private readonly IClipService clipService;
        private readonly IEventAggregator eventAggregator;
        private readonly IRegionManager regionManager;
        private readonly ITemplateService templateService;
        private readonly IWebcamService webcamService;

        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IWebcamService webcamService, IClipService clipService,
            ITemplateService templateService, IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.webcamService = webcamService;
            this.clipService = clipService;
            this.templateService = templateService;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: OnClipsChanged);
            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: OnTemplatesChanged);
            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());

            eventAggregator.GetEvent<ContentUpdatedEvent>().Subscribe(
                action: OnContentUpdated);

            this.WebcamPlayCommand = new DelegateCommand(
                executeMethod: WebcamStartAsync,
                canExecuteMethod: () => !webcamService.IsActive);
            this.WebcamPauseCommand = new DelegateCommand(
                executeMethod: WebcamStopAsync,
                canExecuteMethod: () => webcamService.IsActive);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: ClipAdd,
                canExecuteMethod: () => webcamService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: ClipRemove,
                canExecuteMethod: () => clipService.Selection != default);
            this.ClipAsTemplateCommand = new DelegateCommand(
                executeMethod: ClipAsTemplate,
                canExecuteMethod: () => clipService.Selection?.Content != default);

            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: TemplateRemove,
                canExecuteMethod: () => templateService.Selection != default);
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipAsTemplateCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public bool HasTemplates => templateService.Templates.Any();

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                if (SelectedTabIndex != value)
                {
                    SetProperty(ref selectedTabIndex, value);

                    SetEditRegion();
                }
            }
        }

        public DelegateCommand TemplateRemoveCommand { get; }

        public ObservableCollection<TemplateViewModel> Templates { get; } = new ObservableCollection<TemplateViewModel>();

        public DelegateCommand WebcamPauseCommand { get; }

        public DelegateCommand WebcamPlayCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void ClipAdd()
        {
            clipService.Add();
        }

        private void ClipAsTemplate()
        {
            if (clipService.Selection != default)
            {
                eventAggregator.GetEvent<SelectTemplateEvent>()
                    .Publish(clipService.Selection);
            }
        }

        private void ClipRemove()
        {
            clipService.Remove();
        }

        private void OnClipsChanged()
        {
            ClipAddCommand.RaiseCanExecuteChanged();
            ClipRemoveCommand.RaiseCanExecuteChanged();

            ClipAsTemplateCommand.RaiseCanExecuteChanged();
        }

        private void OnContentUpdated()
        {
            WebcamPlayCommand.RaiseCanExecuteChanged();
            WebcamPauseCommand.RaiseCanExecuteChanged();

            ClipAddCommand.RaiseCanExecuteChanged();
            ClipAsTemplateCommand.RaiseCanExecuteChanged();
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
            SelectedTabIndex = templateService.Selection != default
                ? IndexTemplateView
                : IndexClipView;

            TemplateRemoveCommand.RaiseCanExecuteChanged();
        }

        private void SetEditRegion()
        {
            switch (SelectedTabIndex)
            {
                case IndexClipView:

                    regionManager.RequestNavigate(
                        regionName: RegionNames.EditRegion,
                        source: ViewNames.ClipView);

                    break;

                case IndexTemplateView:

                    regionManager.RequestNavigate(
                        regionName: RegionNames.EditRegion,
                        source: ViewNames.TemplateView);
                    break;
            }
        }

        private void TemplateRemove()
        {
            templateService.RemoveTemplate();
        }

        private async void WebcamStartAsync()
        {
            await webcamService.StartAsync();
        }

        private async void WebcamStopAsync()
        {
            await webcamService.StopAsync();
        }

        #endregion Private Methods
    }
}