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

        private BitmapSource content;
        private ClipViewModel currentClip;
        private bool movedToBottom;
        private bool movedToRight;

        #endregion Private Fields

        #region Public Constructors

        public WebcamViewModel(IWebcamService webcamService, IEventAggregator eventAggregator, IRegionManager regionManager)
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

        public BitmapSource Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
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
                    else if (value > currentClip.Right
                        || (value >= currentClip.Left && movedToRight))
                    {
                        currentClip.Width = value - currentClip.Left.Value;
                        movedToRight = true;
                    }
                    else if (value < currentClip.Left
                        || (value <= currentClip.Right && !movedToRight))
                    {
                        currentClip.Width += currentClip.Left.Value - value;
                        currentClip.Left = value;
                        movedToRight = false;
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
                    else if (value > currentClip.Bottom
                        || (value >= currentClip.Top && movedToBottom))
                    {
                        currentClip.Height = value - currentClip.Top.Value;
                        movedToBottom = true;
                    }
                    else if (value < currentClip.Top
                        || (value <= currentClip.Bottom && !movedToBottom))
                    {
                        currentClip.Height += currentClip.Top.Value - value;
                        currentClip.Top = value;
                        movedToBottom = false;
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

                if (currentClip.Width == 0 || currentClip.Height == 0)
                {
                    Clips.Remove(currentClip);
                }

                currentClip = default;
            }
        }

        private void OnWebcamChanged()
        {
            Content = webcamService.Content;
        }

        #endregion Private Methods
    }
}