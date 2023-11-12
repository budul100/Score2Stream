using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Core;
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
        private readonly INavigationService navigationService;

        private SelectionViewModel activeSelection;
        private Bitmap bitmap;
        private double bitmapHeight;
        private double bitmapWidth;
        private double fullHeight;
        private double fullWidth;
        private double? horizontalMax;
        private double? horizontalMin;
        private double mouseX;
        private double mouseY;
        private bool movedToBottom;
        private bool movedToRight;
        private double? verticalMax;
        private double? verticalMin;
        private double zoom;

        #endregion Private Fields

        #region Public Constructors

        public VideoViewModel(IInputService inputService, IMessageBoxService messageBoxService,
            INavigationService navigationService, IContainerProvider containerProvider, IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.messageBoxService = messageBoxService;
            this.navigationService = navigationService;
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;

            MousePressedCommand = new DelegateCommand<PointerPressedEventArgs>(e => OnMousePressed(e));
            MouseReleasedCommand = new DelegateCommand<PointerReleasedEventArgs>(e => OnMouseReleasedAsync(e));
            ZoomChangedCommand = new DelegateCommand<ZoomChangedEventArgs>(e => OnZoomChanged(e));

            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => UpdateSelections(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => Bitmap = inputService.VideoService?.Bitmap,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<VideoCenteredEvent>().Subscribe(
                action: () => OnVideoCentred(),
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
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler OnVideoCentredEvent;

        #endregion Public Events

        #region Public Properties

        public static double ZoomMax => Constants.ZoomMax;

        public static double ZoomMin => Constants.ZoomMin;

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

        public DelegateCommand<PointerPressedEventArgs> MousePressedCommand { get; }

        public DelegateCommand<PointerReleasedEventArgs> MouseReleasedCommand { get; }

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
                }

                mouseX = value;

                UpdateSelection();
            }
        }

        public double MouseY
        {
            get { return default; }
            set
            {
                if (verticalMin.HasValue)
                {
                    if (value < verticalMin)
                    {
                        value = verticalMin.Value;
                    }
                    else if (value > verticalMax)
                    {
                        value = verticalMax.Value;
                    }
                }

                mouseY = value;

                UpdateSelection();
            }
        }

        public ObservableCollection<SelectionViewModel> Selections { get; } = new ObservableCollection<SelectionViewModel>();

        public DelegateCommand<ZoomChangedEventArgs> ZoomChangedCommand { get; }

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
            var result = ActiveSelection != default
                && Bitmap != default
                && (ActiveSelection.IsEditing || isActivating)
                && navigationService.EditView == ViewType.Clips;

            return result;
        }

        private void OnMousePressed(PointerPressedEventArgs eventArgs)
        {
            if (eventArgs.GetCurrentPoint(default).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed
                && IsMouseEditing(true))
            {
                ActiveSelection.IsEditing = true;
                ActiveSelection.Left = default;
                ActiveSelection.Top = default;
                ActiveSelection.Height = default;
                ActiveSelection.Width = default;

                MouseX = mouseX;
                MouseY = mouseY;
            }
        }

        private async void OnMouseReleasedAsync(PointerReleasedEventArgs eventArgs)
        {
            if (eventArgs.GetCurrentPoint(default).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased
                && IsMouseEditing())
            {
                var dimensionsCanBeSet = ButtonResult.Yes;

                if (ActiveSelection.Clip.HasDimensions
                    && (ActiveSelection.Clip?.Template?.Samples?.Any() == true))
                {
                    dimensionsCanBeSet = await messageBoxService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the dimension of the clip be changed?",
                        contentTitle: "Change dimension");
                }

                if (dimensionsCanBeSet == ButtonResult.Yes)
                {
                    ActiveSelection.Clip.HasDimensions = false;

                    var actualWidth = GetActualWidth();

                    if ((actualWidth ?? 0) > 0
                        && (ActiveSelection.Left ?? 0) >= horizontalMin.Value)
                    {
                        ActiveSelection.Clip.X1Last = ActiveSelection.Clip.X1;
                        ActiveSelection.Clip.X2Last = ActiveSelection.Clip.X2;

                        ActiveSelection.Clip.X1 = ((ActiveSelection.Left ?? 0) - horizontalMin.Value) / actualWidth.Value;
                        ActiveSelection.Clip.X2 = ((ActiveSelection.Left ?? 0) +
                            (ActiveSelection.Width ?? 0) - horizontalMin.Value) / actualWidth.Value;

                        ActiveSelection.Clip.HasDimensions = true;
                    }

                    var actualHeight = GetActualHeight();

                    if ((actualHeight ?? 0) > 0
                        && (ActiveSelection.Top ?? 0) >= verticalMin.Value)
                    {
                        ActiveSelection.Clip.Y1Last = ActiveSelection.Clip.Y1;
                        ActiveSelection.Clip.Y2Last = ActiveSelection.Clip.Y2;

                        ActiveSelection.Clip.Y1 = ((ActiveSelection.Top ?? 0) - verticalMin.Value) / actualHeight.Value;
                        ActiveSelection.Clip.Y2 = ((ActiveSelection.Top ?? 0) - verticalMin.Value +
                            (ActiveSelection.Height ?? 0)) / actualHeight.Value;

                        ActiveSelection.Clip.HasDimensions = true;
                    }

                    ActiveSelection.IsEditing = false;

                    if (ActiveSelection.Clip.HasDimensions)
                    {
                        eventAggregator.GetEvent<ClipUpdatedEvent>().Publish(ActiveSelection.Clip);
                    }
                }
                else
                {
                    SetDimensions();
                }
            }
        }

        private void OnVideoCentred()
        {
            OnVideoCentredEvent.Invoke(
                sender: this,
                e: default);
        }

        private void OnZoomChanged(ZoomChangedEventArgs eventArgs)
        {
            zoom = eventArgs.ZoomX;

            foreach (var selection in Selections)
            {
                selection.Zoom = zoom;
            }
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

        private void UpdateSelection()
        {
            if (IsMouseEditing())
            {
                if (!ActiveSelection.HasValue)
                {
                    ActiveSelection.Left = mouseX;
                    ActiveSelection.Top = mouseY;
                }
                else
                {
                    if (mouseX > (ActiveSelection.Right ?? 0) || (mouseX >= (ActiveSelection.Left ?? 0) && movedToRight))
                    {
                        ActiveSelection.Width = mouseX - ActiveSelection.Left.Value;
                        movedToRight = true;
                    }
                    else if (mouseX < (ActiveSelection.Left ?? 0)
                        || (mouseX <= (ActiveSelection.Right ?? 0) && !movedToRight))
                    {
                        ActiveSelection.Width = (ActiveSelection.Width ?? 0) + ActiveSelection.Left.Value - mouseX;
                        ActiveSelection.Left = mouseX;
                        movedToRight = false;
                    }

                    if (mouseY > (ActiveSelection.Bottom ?? 0) || (mouseY >= (ActiveSelection.Top ?? 0) && movedToBottom))
                    {
                        ActiveSelection.Height = mouseY - ActiveSelection.Top.Value;
                        movedToBottom = true;
                    }
                    else if (mouseY < (ActiveSelection.Top ?? 0)
                        || (mouseY <= (ActiveSelection.Bottom ?? 0) && !movedToBottom))
                    {
                        ActiveSelection.Height = (ActiveSelection.Height ?? 0) + ActiveSelection.Top.Value - mouseY;
                        ActiveSelection.Top = mouseY;
                        movedToBottom = false;
                    }
                }
            }
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
                        zoom: zoom,
                        actualLeft: horizontalMin,
                        actualTop: verticalMin,
                        actualWidth: actualWidth,
                        actualHeight: actualHeight,
                        clipService: inputService.ClipService);

                    Selections.Add(current);

                    if (inputService.ClipService.Active == clip)
                    {
                        ActiveSelection = current;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}