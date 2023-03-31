using Prism.Commands;
using Prism.Regions;
using ScoreboardOCR.Core.Constants;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace WebcamModule.ViewModels
{
    public class WebcamViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int BorderThicknessDefault = 2;

        private readonly IClipService clipService;
        private readonly IRegionManager regionManager;
        private readonly IWebcamService webcamService;

        private ClipViewModel activeClip;
        private int borderThickness;
        private BitmapSource content;
        private double contentHeight;
        private double contentWidth;
        private double fullHeight;
        private double fullWidth;
        private bool isMouseActive;
        private bool movedToBottom;
        private bool movedToRight;
        private double? x1;
        private double? x2;
        private double? y1;
        private double? y2;

        #endregion Private Fields

        #region Public Constructors

        public WebcamViewModel(IWebcamService webcamService, IClipService clipService, IRegionManager regionManager)
            : base(regionManager)
        {
            this.webcamService = webcamService;
            this.clipService = clipService;
            this.regionManager = regionManager;

            webcamService.OnContentChangedEvent += OnContentChanged;

            clipService.OnClipsChangedEvent += OnClipsChanged;
            clipService.OnClipsUpdatedEvent += OnClipsUpdated;

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

        public double ContentHeight
        {
            get { return contentHeight; }
            set
            {
                SetProperty(ref contentHeight, value);
                SetDimensions();
            }
        }

        public double ContentWidth
        {
            get { return contentWidth; }
            set
            {
                SetProperty(ref contentWidth, value);
                SetDimensions();
            }
        }

        public double FullHeight
        {
            get { return fullHeight; }
            set
            {
                SetProperty(ref fullHeight, value);
                SetDimensions();
            }
        }

        public double FullWidth
        {
            get { return fullWidth; }
            set
            {
                SetProperty(ref fullWidth, value);
                SetDimensions();
            }
        }

        public DelegateCommand MouseDownCommand { get; }

        public DelegateCommand MouseUpCommand { get; }

        public double MouseX
        {
            get { return default; }
            set
            {
                if (IsMouseEditing()
                    && x1.HasValue
                    && value >= x1
                    && value <= x2)
                {
                    if (!activeClip.HasValue)
                    {
                        activeClip.Left = value;
                    }
                    else if (value > (activeClip.Right ?? 0)
                        || (value >= (activeClip.Left ?? 0) && movedToRight))
                    {
                        activeClip.Width = value - activeClip.Left.Value;
                        movedToRight = true;
                    }
                    else if (value < (activeClip.Left ?? 0)
                        || (value <= (activeClip.Right ?? 0) && !movedToRight))
                    {
                        activeClip.Width = (activeClip.Width ?? 0) + activeClip.Left.Value - value;
                        activeClip.Left = value;
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
                if (IsMouseEditing()
                    && y1.HasValue
                    && value >= y1
                    && value <= y2)
                {
                    if (!activeClip.HasValue)
                    {
                        activeClip.Top = value;
                    }
                    else if (value > (activeClip.Bottom ?? 0)
                        || (value >= (activeClip.Top ?? 0) && movedToBottom))
                    {
                        activeClip.Height = value - activeClip.Top.Value;
                        movedToBottom = true;
                    }
                    else if (value < (activeClip.Top ?? 0)
                        || (value <= (activeClip.Bottom ?? 0) && !movedToBottom))
                    {
                        activeClip.Height = (activeClip.Height ?? 0) + activeClip.Top.Value - value;
                        activeClip.Top = value;
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

        private double? GetActualHeight()
        {
            var result = y1.HasValue
                ? y2.Value - y1.Value
                : default(double?);

            return result;
        }

        private double? GetActualWidth()
        {
            var result = x1.HasValue
                ? x2.Value - x1.Value
                : default(double?);

            return result;
        }

        private bool IsMouseEditing(bool isActivating = false)
        {
            var result = (isMouseActive || isActivating)
                && activeClip != default
                && Content != default
                && regionManager.Regions[RegionNames.EditRegion]?.NavigationService.Journal
                    .CurrentEntry.Uri.OriginalString == ViewNames.ClipView;

            return result;
        }

        private void OnClipsChanged(object sender, System.EventArgs e)
        {
            SetClips();
        }

        private void OnClipsUpdated(object sender, EventArgs e)
        {
            if (webcamService.IsActive)
            {
                foreach (var clip in Clips)
                {
                    clip.IsActive = clip.Clip == clipService.Selection;

                    if (clip.IsActive)
                    {
                        activeClip = clip;
                    }

                    clip.Update();
                }
            }
        }

        private void OnContentChanged(object sender, EventArgs e)
        {
            Content = webcamService.Content;
        }

        private void OnMouseDown()
        {
            if (IsMouseEditing(
                isActivating: true))
            {
                activeClip.Left = default;
                activeClip.Top = default;
                activeClip.Height = default;
                activeClip.Width = default;

                isMouseActive = true;
            }
        }

        private void OnMouseUp()
        {
            if (IsMouseEditing())
            {
                var actualWidth = GetActualWidth();

                if ((actualWidth ?? 0) > 0
                    && (activeClip.Left ?? 0) >= x1.Value)
                {
                    activeClip.Clip.RelativeX1 = ((activeClip.Left ?? 0) - x1.Value) / actualWidth.Value;
                    activeClip.Clip.RelativeX2 = ((activeClip.Left ?? 0) + (activeClip.Width ?? 0) - x1.Value) / actualWidth.Value;
                }

                var actualHeight = GetActualHeight();

                if ((actualHeight ?? 0) > 0
                    && (activeClip.Top ?? 0) >= y1.Value)
                {
                    activeClip.Clip.RelativeY1 = ((activeClip.Top ?? 0) - y1.Value) / actualHeight.Value;
                    activeClip.Clip.RelativeY2 = ((activeClip.Top ?? 0) - y1.Value + (activeClip.Height ?? 0)) / actualHeight.Value;
                }

                clipService.Define();
            }

            isMouseActive = false;
        }

        private void SetClips()
        {
            Clips.Clear();

            var actualWidth = GetActualWidth();
            var actualHeight = GetActualHeight();

            if (webcamService.IsActive
                && actualWidth.HasValue
                && actualHeight.HasValue)
            {
                foreach (var clip in clipService.Clips)
                {
                    var current = new ClipViewModel(
                        clip: clip,
                        actualLeft: x1,
                        actualTop: y1,
                        actualWidth: actualWidth,
                        actualHeight: actualHeight);

                    Clips.Add(current);
                }
            }
        }

        private void SetDimensions()
        {
            if (ContentHeight == 0 || ContentWidth == 0)
            {
                x1 = default;
                x2 = default;
                y1 = default;
                y2 = default;
            }
            else
            {
                x1 = BorderThickness + Math.Floor((FullWidth - ContentWidth) / 2);
                x2 = x1 + Math.Floor(ContentWidth);

                y1 = BorderThickness + Math.Floor((FullHeight - ContentHeight) / 2);
                y2 = y1 + Math.Floor(ContentHeight);
            }

            SetClips();
        }

        #endregion Private Methods
    }
}