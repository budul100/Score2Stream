using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using ScoreboardOCR.Core.Events;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Windows.Input;

namespace MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IWebcamService webcamService;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IWebcamService webcamService, IEventAggregator eventAggregator, IRegionManager regionManager)
            : base(regionManager)
        {
            this.eventAggregator = eventAggregator;
            this.webcamService = webcamService;

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
            eventAggregator
                .GetEvent<ClipAddEvent>()
                .Publish();
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