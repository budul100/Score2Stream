using Core.Enums;
using Core.Events;
using Core.Events.Video;
using Core.Interfaces;
using Core.Prism;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace VideoModule.ViewModels
{
    public class VideoViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int BorderThicknessDefault = 2;

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IInputService inputService;
        private readonly IRegionManager regionManager;

        private SelectionViewModel activeSelection;
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

        public VideoViewModel(IInputService inputService, IContainerProvider containerProvider,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.containerProvider = containerProvider;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => activeSelection = Selections.SingleOrDefault(v => v.Clip == c),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: UpdateSelections,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => Content = inputService.VideoService?.Bitmap,
                keepSubscriberReferenceAlive: true);

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
                    if (!activeSelection.HasValue)
                    {
                        activeSelection.Left = value;
                    }
                    else if (value > (activeSelection.Right ?? 0)
                        || (value >= (activeSelection.Left ?? 0) && movedToRight))
                    {
                        activeSelection.Width = value - activeSelection.Left.Value;
                        movedToRight = true;
                    }
                    else if (value < (activeSelection.Left ?? 0)
                        || (value <= (activeSelection.Right ?? 0) && !movedToRight))
                    {
                        activeSelection.Width = (activeSelection.Width ?? 0) + activeSelection.Left.Value - value;
                        activeSelection.Left = value;
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
                    if (!activeSelection.HasValue)
                    {
                        activeSelection.Top = value;
                    }
                    else if (value > (activeSelection.Bottom ?? 0)
                        || (value >= (activeSelection.Top ?? 0) && movedToBottom))
                    {
                        activeSelection.Height = value - activeSelection.Top.Value;
                        movedToBottom = true;
                    }
                    else if (value < (activeSelection.Top ?? 0)
                        || (value <= (activeSelection.Bottom ?? 0) && !movedToBottom))
                    {
                        activeSelection.Height = (activeSelection.Height ?? 0) + activeSelection.Top.Value - value;
                        activeSelection.Top = value;
                        movedToBottom = false;
                    }
                }
            }
        }

        public ObservableCollection<SelectionViewModel> Selections { get; } = new ObservableCollection<SelectionViewModel>();

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
                && activeSelection != default
                && Content != default
                && regionManager.Regions[nameof(RegionType.EditRegion)]?.NavigationService.Journal
                    .CurrentEntry.Uri.OriginalString == nameof(ViewType.Clips);

            return result;
        }

        private void OnMouseDown()
        {
            if (IsMouseEditing(
                isActivating: true))
            {
                activeSelection.Left = default;
                activeSelection.Top = default;
                activeSelection.Height = default;
                activeSelection.Width = default;

                isMouseActive = true;
            }
        }

        private void OnMouseUp()
        {
            if (IsMouseEditing())
            {
                activeSelection.Clip.HasDimensions = false;

                var actualWidth = GetActualWidth();

                if ((actualWidth ?? 0) > 0
                    && (activeSelection.Left ?? 0) >= x1.Value)
                {
                    activeSelection.Clip.RelativeX1 = ((activeSelection.Left ?? 0) - x1.Value) / actualWidth.Value;
                    activeSelection.Clip.RelativeX2 = ((activeSelection.Left ?? 0) +
                        (activeSelection.Width ?? 0) - x1.Value) / actualWidth.Value;

                    activeSelection.Clip.HasDimensions = true;
                }

                var actualHeight = GetActualHeight();

                if ((actualHeight ?? 0) > 0
                    && (activeSelection.Top ?? 0) >= y1.Value)
                {
                    activeSelection.Clip.RelativeY1 = ((activeSelection.Top ?? 0) - y1.Value) / actualHeight.Value;
                    activeSelection.Clip.RelativeY2 = ((activeSelection.Top ?? 0) - y1.Value +
                        (activeSelection.Height ?? 0)) / actualHeight.Value;

                    activeSelection.Clip.HasDimensions = true;
                }

                if (activeSelection.Clip.HasDimensions)
                {
                    eventAggregator.GetEvent<ClipUpdatedEvent>()
                        .Publish(activeSelection.Clip);
                }
            }

            isMouseActive = false;
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

            UpdateSelections();
        }

        private void UpdateSelections()
        {
            Selections.Clear();

            var actualWidth = GetActualWidth();
            var actualHeight = GetActualHeight();

            if (inputService.IsActive
                && actualWidth.HasValue
                && actualHeight.HasValue)
            {
                foreach (var clip in inputService.ClipService.Clips)
                {
                    var current = containerProvider.Resolve<SelectionViewModel>();

                    current.Initialize(
                        clip: clip,
                        actualLeft: x1,
                        actualTop: y1,
                        actualWidth: actualWidth,
                        actualHeight: actualHeight);

                    Selections.Add(current);
                }
            }
        }

        #endregion Private Methods
    }
}