using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using ScoreboardOCR.Core.Events;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WebcamModule.ViewModels
{
    public class WebcamViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IWebcamService webcamService;
        private BitmapSource currentView;

        #endregion Private Fields

        #region Public Constructors

        public WebcamViewModel(IRegionManager regionManager, IEventAggregator eventAggregator,
            IWebcamService webcamService)
            : base(regionManager)
        {
            this.webcamService = webcamService;

            eventAggregator
                .GetEvent<WebcamChangedEvent>()
                .Subscribe(OnWebcamChanged);

            StartCommand = new DelegateCommand(StartAsync);
            StopCommand = new DelegateCommand(StopAsync);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource CurrentView
        {
            get { return currentView; }
            set { SetProperty(ref currentView, value); }
        }

        public ICommand StartCommand { get; }

        public ICommand StopCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            //do something
        }

        #endregion Public Methods

        #region Private Methods

        private void OnWebcamChanged()
        {
            CurrentView = webcamService.Current;
        }

        private async void StartAsync()
        {
            await webcamService.StartAsync();
        }

        private async void StopAsync()
        {
            await webcamService.StopAsync();
        }

        #endregion Private Methods
    }
}