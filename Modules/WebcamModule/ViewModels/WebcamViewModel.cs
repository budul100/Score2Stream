using Prism.Commands;
using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WebcamModule.ViewModels
{
    public class WebcamViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int BorderThicknessDefault = 2;

        private readonly IClipService clipService;
        private readonly IWebcamService webcamService;

        private ClipViewModel activeClip;
        private int borderThickness;
        private BitmapSource content;
        private int fullHeight;
        private int fullWidth;
        private int imageHeight;
        private int imageWidth;
        private bool isMouseActive;
        private bool movedToBottom;
        private bool movedToRight;

        #endregion Private Fields

        #region Public Constructors

        public WebcamViewModel(IWebcamService webcamService, IClipService clipService, IRegionManager regionManager)
            : base(regionManager)
        {
            this.webcamService = webcamService;
            this.clipService = clipService;

            webcamService.OnContentChangedEvent += OnContentChanged;
            clipService.OnClipsChangedEvent += OnClipsChanged;
            clipService.OnClipActivatedEvent += OnClipActivated;

            MouseDownCommand = new DelegateCommand(OnMouseDown);
            MouseUpCommand = new DelegateCommand(OnMouseUp);

            BorderThickness = BorderThicknessDefault;
        }

        #endregion Public Constructors

        #region Public Properties

        public int BorderThickness
        {
            get { return borderThickness; }
            set { SetProperty(ref borderThickness, value); }
        }

        public ObservableCollection<ClipViewModel> Clips { get; } = new ObservableCollection<ClipViewModel>();

        public BitmapSource Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        public int FullHeight
        {
            get { return fullHeight; }
            set { SetProperty(ref fullHeight, value); }
        }

        public int FullWidth
        {
            get { return fullWidth; }
            set { SetProperty(ref fullWidth, value); }
        }

        public int ImageHeight
        {
            get { return imageHeight; }
            set { SetProperty(ref imageHeight, value); }
        }

        public int ImageWidth
        {
            get { return imageWidth; }
            set { SetProperty(ref imageWidth, value); }
        }

        public ICommand MouseDownCommand { get; }

        public ICommand MouseUpCommand { get; }

        public double MouseX
        {
            get { return default; }
            set
            {
                if (isMouseActive
                    && activeClip != default
                    && Content != default)
                {
                    if (!activeClip.HasValue)
                    {
                        activeClip.Left = value;
                    }
                    else if (value > activeClip.Right
                        || (value >= activeClip.Left && movedToRight))
                    {
                        activeClip.Width = value - activeClip.Left.Value;
                        movedToRight = true;
                    }
                    else if (value < activeClip.Left
                        || (value <= activeClip.Right && !movedToRight))
                    {
                        activeClip.Width += activeClip.Left.Value - value;
                        activeClip.Left = value;
                        movedToRight = false;
                    }

                    activeClip.Clip.BoxLeft = activeClip.Left;
                    activeClip.Clip.BoxTop = activeClip.Top;
                    activeClip.Clip.BoxWidth = activeClip.Width;
                    activeClip.Clip.BoxHeight = activeClip.Height;
                }
            }
        }

        public double MouseY
        {
            get { return default; }
            set
            {
                if (isMouseActive
                    && activeClip != default
                    && Content != default)
                {
                    if (!activeClip.HasValue)
                    {
                        activeClip.Top = value;
                    }
                    else if (value > activeClip.Bottom
                        || (value >= activeClip.Top && movedToBottom))
                    {
                        activeClip.Height = value - activeClip.Top.Value;
                        movedToBottom = true;
                    }
                    else if (value < activeClip.Top
                        || (value <= activeClip.Bottom && !movedToBottom))
                    {
                        activeClip.Height += activeClip.Top.Value - value;
                        activeClip.Top = value;
                        movedToBottom = false;
                    }

                    activeClip.Clip.BoxLeft = activeClip.Left;
                    activeClip.Clip.BoxTop = activeClip.Top;
                    activeClip.Clip.BoxWidth = activeClip.Width;
                    activeClip.Clip.BoxHeight = activeClip.Height;
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void OnClipActivated(object sender, EventArgs e)
        {
            activeClip = Clips
                .SingleOrDefault(c => c.Clip == clipService.Active);
        }

        private void OnClipsChanged(object sender, System.EventArgs e)
        {
            Clips.Clear();

            foreach (var clip in clipService.Clips)
            {
                var current = new ClipViewModel(clip);
                Clips.Add(current);
            }
        }

        private void OnContentChanged(object sender, EventArgs e)
        {
            Content = webcamService.Content;
        }

        private void OnMouseDown()
        {
            if (activeClip != default
                && Content != default)
            {
                activeClip.Left = default;
                activeClip.Top = default;

                isMouseActive = true;
            }
        }

        private void OnMouseUp()
        {
            isMouseActive = false;
        }

        #endregion Private Methods
    }
}