using Prism.Commands;
using Prism.Regions;
using ScoreboardOCR.Core.Constants;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System;
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
        private readonly IRegionManager regionManager;
        private readonly ITemplateService templateService;
        private readonly IWebcamService webcamService;

        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IWebcamService webcamService, IClipService clipService,
            ITemplateService templateService, IRegionManager regionManager)
            : base(regionManager)
        {
            this.webcamService = webcamService;
            this.clipService = clipService;
            this.templateService = templateService;
            this.regionManager = regionManager;

            webcamService.OnContentChangedEvent += OnContentChanged;

            clipService.OnClipsChangedEvent += OnClipsChanged;
            clipService.OnClipsUpdatedEvent += OnClipsUpdated;

            templateService.OnTemplatesChangedEvent += OnTemplatesChanged;
            templateService.OnTemplatesUpdatedEvent += OnTemplatesUpdated;

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
                templateService.Select(clipService.Selection);
            }
        }

        private void ClipRemove()
        {
            clipService.Remove();
        }

        private void OnClipsChanged(object sender, System.EventArgs e)
        {
            ClipAddCommand.RaiseCanExecuteChanged();
            ClipRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnClipsUpdated(object sender, EventArgs e)
        {
            UpdateTemplates();
        }

        private void OnContentChanged(object sender, System.EventArgs e)
        {
            WebcamPlayCommand.RaiseCanExecuteChanged();
            WebcamPauseCommand.RaiseCanExecuteChanged();

            ClipAddCommand.RaiseCanExecuteChanged();
            ClipAsTemplateCommand.RaiseCanExecuteChanged();
        }

        private void OnTemplatesChanged(object sender, System.EventArgs e)
        {
            SetTemplates();

            UpdateTemplatesMenu();
        }

        private void OnTemplatesUpdated(object sender, EventArgs e)
        {
            UpdateTemplatesMenu();
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

        private void SetTemplates()
        {
            Templates.Clear();

            foreach (var template in templateService.Templates)
            {
                var current = new TemplateViewModel(
                    templateService: templateService,
                    clip: template.Clip);

                Templates.Add(current);
            }
        }

        private void TemplateRemove()
        {
            templateService.Remove();
        }

        private void UpdateTemplates()
        {
            foreach (var template in Templates)
            {
                template.Update();
            }
        }

        private void UpdateTemplatesMenu()
        {
            SelectedTabIndex = templateService.Selection != default
                ? IndexTemplateView
                : IndexClipView;

            RaisePropertyChanged(nameof(HasTemplates));

            TemplateRemoveCommand.RaiseCanExecuteChanged();
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