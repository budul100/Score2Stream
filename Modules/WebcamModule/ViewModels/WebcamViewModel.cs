using Core.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using ScoreboardOCR.Core.Events;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WebcamModule.ViewModels
{
    public class WebcamViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IWebcamService webcamService;

        private ClipViewModel currentClip;
        private BitmapSource currentView;

        #endregion Private Fields

        #region Public Constructors

        public WebcamViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, IWebcamService webcamService)
            : base(regionManager)
        {
            this.webcamService = webcamService;

            eventAggregator
                .GetEvent<WebcamChangedEvent>()
                .Subscribe(OnWebcamChanged);

            eventAggregator
                .GetEvent<ClipAddEvent>()
                .Subscribe(OnAddClip);

            MouseDownCommand = new DelegateCommand(OnMouseDown);
            MouseUpCommand = new DelegateCommand(OnMouseUp);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<ClipViewModel> Clips { get; } = new ObservableCollection<ClipViewModel>();

        public BitmapSource CurrentView
        {
            get { return currentView; }
            set { SetProperty(ref currentView, value); }
        }

        public ICommand MouseDownCommand { get; }

        public ICommand MouseUpCommand { get; }

        public double MouseX
        {
            get { return default; }
            set
            {
                if (currentClip?.IsActive == true)
                {
                    if (!currentClip.HasValue)
                    {
                        currentClip.Left = value;
                    }
                    else
                    {
                        currentClip.Width = value - currentClip.Left.Value;
                    }
                }
            }
        }

        public double MouseY
        {
            get { return default; }
            set
            {
                if (currentClip?.IsActive == true)
                {
                    if (!currentClip.HasValue)
                    {
                        currentClip.Top = value;
                    }
                    else
                    {
                        currentClip.Height = value - currentClip.Top.Value;
                    }
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void OnAddClip()
        {
            currentClip = new ClipViewModel();

            Clips.Add(currentClip);
        }

        private void OnMouseDown()
        {
            if (currentClip != default)
            {
                currentClip.IsActive = true;
            }
        }

        private void OnMouseUp()
        {
            if (currentClip?.IsActive == true)
            {
                currentClip.IsActive = false;
                currentClip = default;
            }
        }

        private void OnWebcamChanged()
        {
            CurrentView = webcamService.Current;
        }

        #endregion Private Methods
    }
}