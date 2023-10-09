using Avalonia.Media.Imaging;
using MessageBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Prism;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Score2Stream.VideoModule.ViewModels
{
    public class VideoViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IInputService inputService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IRegionManager regionManager;

        private SelectionViewModel activeSelection;
        private Bitmap bitmap;
        private double bitmapHeight;
        private double bitmapWidth;
        private double fullHeight;
        private double fullWidth;
        private double? horizontalMax;
        private double? horizontalMin;
        private bool isMouseActive;
        private bool movedToBottom;
        private bool movedToRight;
        private double? verticalMax;
        private double? verticalMin;

        #endregion Private Fields

        #region Public Constructors

        public VideoViewModel(IInputService inputService, IMessageBoxService messageBoxService,
            IContainerProvider containerProvider, IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.messageBoxService = messageBoxService;
            this.containerProvider = containerProvider;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => UpdateSelections(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => ActiveSelection = Selections.SingleOrDefault(v => v.Clip == c),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: UpdateSelections,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => UpdateSelections(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => Bitmap = inputService.VideoService?.Bitmap,
                keepSubscriberReferenceAlive: true);

            MousePressedCommand = new DelegateCommand(OnMousePressed);
            MouseReleasedCommand = new DelegateCommand(OnMouseReleasedAsync);
        }

        #endregion Public Constructors

        #region Public Properties

        public SelectionViewModel ActiveSelection
        {
            get { return activeSelection; }
            set { SetProperty(ref activeSelection, value); }
        }

        public Bitmap Bitmap
        {
            get { return bitmap; }
            set { SetProperty(ref bitmap, value); }
        }

        public double BitmapHeight
        {
            get { return bitmapHeight; }
            set
            {
                SetProperty(ref bitmapHeight, value);
                SetDimensions();
            }
        }

        public double BitmapWidth
        {
            get { return bitmapWidth; }
            set
            {
                SetProperty(ref bitmapWidth, value);
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

        public DelegateCommand MousePressedCommand { get; }

        public DelegateCommand MouseReleasedCommand { get; }

        public double MouseX
        {
            get { return default; }
            set
            {
                if (IsMouseEditing()
                    && horizontalMin.HasValue)
                {
                    if (value < horizontalMin)
                    {
                        value = horizontalMin.Value;
                    }
                    else if (value > horizontalMax)
                    {
                        value = horizontalMax.Value;
                    }

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
                    && verticalMin.HasValue)
                {
                    if (value < verticalMin)
                    {
                        value = verticalMin.Value;
                    }
                    else if (value > verticalMax)
                    {
                        value = verticalMax.Value;
                    }

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
            var result = verticalMin.HasValue
                ? verticalMax.Value - verticalMin.Value
                : default(double?);

            return result;
        }

        private double? GetActualWidth()
        {
            var result = horizontalMin.HasValue
                ? horizontalMax.Value - horizontalMin.Value
                : default(double?);

            return result;
        }

        private bool IsMouseEditing(bool isActivating = false)
        {
            var result = (isMouseActive || isActivating)
                && activeSelection != default
                && Bitmap != default
                && regionManager.Regions[nameof(RegionType.EditRegion)]?.NavigationService.Journal
                    .CurrentEntry.Uri.OriginalString == nameof(ViewType.Clips);

            return result;
        }

        private void OnMousePressed()
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

        private async void OnMouseReleasedAsync()
        {
            if (IsMouseEditing())
            {
                var dimensionsCanBeSet = ButtonResult.Yes;

                if (activeSelection.Clip.HasDimensions
                    && (activeSelection.Clip?.Template?.Samples?.Any() == true))
                {
                    dimensionsCanBeSet = await messageBoxService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the dimension of the clip be changed?",
                        contentTitle: "Change dimension");
                }

                if (dimensionsCanBeSet == ButtonResult.Yes)
                {
                    activeSelection.Clip.HasDimensions = false;

                    var actualWidth = GetActualWidth();

                    if ((actualWidth ?? 0) > 0
                        && (activeSelection.Left ?? 0) >= horizontalMin.Value)
                    {
                        activeSelection.Clip.RelativeX1 = ((activeSelection.Left ?? 0) - horizontalMin.Value) / actualWidth.Value;
                        activeSelection.Clip.RelativeX2 = ((activeSelection.Left ?? 0) +
                            (activeSelection.Width ?? 0) - horizontalMin.Value) / actualWidth.Value;

                        activeSelection.Clip.HasDimensions = true;
                    }

                    var actualHeight = GetActualHeight();

                    if ((actualHeight ?? 0) > 0
                        && (activeSelection.Top ?? 0) >= verticalMin.Value)
                    {
                        activeSelection.Clip.RelativeY1 = ((activeSelection.Top ?? 0) - verticalMin.Value) / actualHeight.Value;
                        activeSelection.Clip.RelativeY2 = ((activeSelection.Top ?? 0) - verticalMin.Value +
                            (activeSelection.Height ?? 0)) / actualHeight.Value;

                        activeSelection.Clip.HasDimensions = true;
                    }

                    if (activeSelection.Clip.HasDimensions)
                    {
                        eventAggregator.GetEvent<ClipUpdatedEvent>().Publish(
                            payload: activeSelection.Clip);
                    }
                }
                else
                {
                    SetDimensions();
                }
            }

            isMouseActive = false;
        }

        private void SetDimensions()
        {
            if (BitmapHeight == 0 || BitmapWidth == 0)
            {
                horizontalMin = default;
                horizontalMax = default;
                verticalMin = default;
                verticalMax = default;
            }
            else
            {
                horizontalMin = Math.Floor((FullWidth - BitmapWidth) / 2);
                horizontalMax = horizontalMin + Math.Floor(BitmapWidth);

                verticalMin = Math.Floor((FullHeight - BitmapHeight) / 2);
                verticalMax = verticalMin + Math.Floor(BitmapHeight);
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

                    var isActive = inputService.ClipService.Active == clip;

                    current.Initialize(
                        clip: clip,
                        isActive: isActive,
                        actualLeft: horizontalMin,
                        actualTop: verticalMin,
                        actualWidth: actualWidth,
                        actualHeight: actualHeight);

                    Selections.Add(current);

                    if (isActive)
                    {
                        ActiveSelection = current;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}