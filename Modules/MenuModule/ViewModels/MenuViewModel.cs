using Prism.Commands;
using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Windows.Input;

namespace MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IClipService clipService;
        private readonly IWebcamService webcamService;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IWebcamService webcamService, IClipService clipService, IRegionManager regionManager)
            : base(regionManager)
        {
            this.webcamService = webcamService;
            this.clipService = clipService;

            this.ClipAddCommand = new DelegateCommand(ClipAdd);
            this.WebcamPlayCommand = new DelegateCommand(WebcamStartAsync);
            this.WebcamPauseCommand = new DelegateCommand(WebcamStopAsync);
        }

        #endregion Public Constructors

        #region Public Properties

        public ICommand ClipAddCommand { get; }

        public ICommand WebcamPauseCommand { get; }

        public ICommand WebcamPlayCommand { get; }

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