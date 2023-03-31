using Prism.Commands;
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

        private const int IndexClipView = 0;
        private const int IndexTemplateView = 1;

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
            templateService.OnTemplatesChangedEvent += OnTemplatesChanged;

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
                canExecuteMethod: () => clipService.Active != default);

            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: TemplateRemove,
                canExecuteMethod: () => templateService.Active != default);
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

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

        private void ClipRemove()
        {
            clipService.Remove();
        }

        private void OnClipsChanged(object sender, System.EventArgs e)
        {
            SetTemplates();

            ClipAddCommand.RaiseCanExecuteChanged();
            ClipRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnContentChanged(object sender, System.EventArgs e)
        {
            WebcamPlayCommand.RaiseCanExecuteChanged();
            WebcamPauseCommand.RaiseCanExecuteChanged();

            ClipAddCommand.RaiseCanExecuteChanged();
        }

        private void OnTemplatesChanged(object sender, System.EventArgs e)
        {
            SetTemplates();

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

        private void SetTemplates()
        {
            Templates.Clear();

            foreach (var clip in clipService.Clips)
            {
                var isActive = templateService.Templates
                    .Any(t => t.Clip == clip);

                var template = new TemplateViewModel(
                    templateService: templateService,
                    clip: clip,
                    isActive: isActive);

                Templates.Add(template);
            }
        }

        private void TemplateRemove()
        {
            templateService.Remove();
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