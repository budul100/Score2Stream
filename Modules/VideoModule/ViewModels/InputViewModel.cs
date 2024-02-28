using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Video;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Score2Stream.VideoModule.ViewModels
{
    public class InputViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IDialogService dialogService;
        private readonly IInputService inputService;
        private readonly INavigationService navigationService;

        private AreaViewModel area;
        private Bitmap bitmap;
        private double bitmapHeight;
        private double bitmapWidth;
        private double heightFull;
        private double? heightMax;
        private double? heightMin;
        private double mouseX;
        private double mouseY;
        private bool movedToBottom;
        private bool movedToRight;
        private double widthFull;
        private double? widthMax;
        private double? widthMin;
        private double zoom;

        #endregion Private Fields

        #region Public Constructors

        public InputViewModel(IInputService inputService, INavigationService navigationService,
            IDialogService dialogService, IContainerProvider containerProvider, IEventAggregator eventAggregator,
            IRegionManager regionManager)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.navigationService = navigationService;
            this.dialogService = dialogService;
            this.containerProvider = containerProvider;

            MousePressedCommand = new DelegateCommand<PointerPressedEventArgs>(e => OnMousePressed(e));
            MouseReleasedCommand = new DelegateCommand<PointerReleasedEventArgs>(e => OnMouseReleasedAsync(e));
            ZoomChangedCommand = new DelegateCommand<ZoomChangedEventArgs>(e => OnZoomChanged(e));

            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => UpdateAreas(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => Bitmap = inputService.VideoService?.Bitmap,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<VideoCenteredEvent>().Subscribe(
                action: () => OnVideoCentred(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: a => area = Areas.SingleOrDefault(m => a == m.Area),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: UpdateAreas,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => UpdateAreas(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler OnVideoCentredEvent;

        #endregion Public Events

        #region Public Properties

        public static double ZoomMax => Constants.ZoomMax;

        public static double ZoomMin => Constants.ZoomMin;

        public ObservableCollection<AreaViewModel> Areas { get; } = new ObservableCollection<AreaViewModel>();

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
            get { return heightFull; }
            set
            {
                SetProperty(ref heightFull, value);
                SetDimensions();
            }
        }

        public double FullWidth
        {
            get { return widthFull; }
            set
            {
                SetProperty(ref widthFull, value);
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
                    && widthMin.HasValue)
                {
                    if (value < widthMin)
                    {
                        value = widthMin.Value;
                    }
                    else if (value > widthMax)
                    {
                        value = widthMax.Value;
                    }
                }

                mouseX = value;

                UpdateArea();
            }
        }

        public double MouseY
        {
            get { return default; }
            set
            {
                if (heightMin.HasValue)
                {
                    if (value < heightMin)
                    {
                        value = heightMin.Value;
                    }
                    else if (value > heightMax)
                    {
                        value = heightMax.Value;
                    }
                }

                mouseY = value;

                UpdateArea();
            }
        }

        public DelegateCommand<ZoomChangedEventArgs> ZoomChangedCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private double? GetActualHeight()
        {
            var result = heightMin.HasValue
                ? heightMax.Value - heightMin.Value
                : default(double?);

            return result;
        }

        private double? GetActualWidth()
        {
            var result = widthMin.HasValue
                ? widthMax.Value - widthMin.Value
                : default(double?);

            return result;
        }

        private bool IsMouseEditing(bool isActivating = false)
        {
            var result = area != default
                && Bitmap != default
                && (area.IsEditing || isActivating)
                && navigationService.EditView == ViewType.Areas;

            return result;
        }

        private void OnMousePressed(PointerPressedEventArgs eventArgs)
        {
            if (eventArgs.GetCurrentPoint(default).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed
                && IsMouseEditing(true))
            {
                area.IsEditing = true;
                area.Left = default;
                area.Top = default;
                area.Height = default;
                area.Width = default;

                MouseX = mouseX;
                MouseY = mouseY;
            }
        }

        private async void OnMouseReleasedAsync(PointerReleasedEventArgs eventArgs)
        {
            if (eventArgs.GetCurrentPoint(default).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased
                && IsMouseEditing())
            {
                var isResized = false;

                if (area.IsVisible)
                {
                    var canBeResized = !area.Area.HasDimensions
                        || area.Area?.Template?.Samples?.Any() != true;

                    if (!canBeResized)
                    {
                        var messageBoxResult = await dialogService.GetMessageBoxResultAsync(
                            contentMessage: "Shall the dimension of the clip be changed?",
                            contentTitle: "Change dimension");

                        canBeResized = messageBoxResult == ButtonResult.Yes;
                    }

                    if (canBeResized)
                    {
                        var actualWidth = GetActualWidth();
                        var actualHeight = GetActualHeight();

                        inputService.AreaService.Resize(
                            left: area.Left,
                            widthMin: widthMin,
                            widthFull: area.Width,
                            widthActual: actualWidth,
                            top: area.Top,
                            heightMin: heightMin,
                            heightFull: area.Height,
                            heightActual: actualHeight);

                        area.IsEditing = false;

                        isResized = true;
                    }
                }

                if (!isResized)
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

            foreach (var area in Areas)
            {
                area.Zoom = zoom;
            }
        }

        private void SetDimensions()
        {
            if (BitmapHeight == 0 || BitmapWidth == 0)
            {
                widthMin = default;
                widthMax = default;
                heightMin = default;
                heightMax = default;
            }
            else
            {
                widthMin = Math.Floor((FullWidth - BitmapWidth) / 2);
                widthMax = widthMin + Math.Floor(BitmapWidth);

                heightMin = Math.Floor((FullHeight - BitmapHeight) / 2);
                heightMax = heightMin + Math.Floor(BitmapHeight);
            }

            UpdateAreas();
        }

        private void UpdateArea()
        {
            if (IsMouseEditing())
            {
                if (!area.HasValue)
                {
                    area.Left = mouseX;
                    area.Top = mouseY;
                }
                else
                {
                    if (mouseX > (area.Right ?? 0) || (mouseX >= (area.Left ?? 0) && movedToRight))
                    {
                        area.Width = mouseX - area.Left.Value;
                        movedToRight = true;
                    }
                    else if (mouseX < (area.Left ?? 0)
                        || (mouseX <= (area.Right ?? 0) && !movedToRight))
                    {
                        area.Width = (area.Width ?? 0) + area.Left.Value - mouseX;
                        area.Left = mouseX;
                        movedToRight = false;
                    }

                    if (mouseY > (area.Bottom ?? 0) || (mouseY >= (area.Top ?? 0) && movedToBottom))
                    {
                        area.Height = mouseY - area.Top.Value;
                        movedToBottom = true;
                    }
                    else if (mouseY < (area.Top ?? 0)
                        || (mouseY <= (area.Bottom ?? 0) && !movedToBottom))
                    {
                        area.Height = (area.Height ?? 0) + area.Top.Value - mouseY;
                        area.Top = mouseY;
                        movedToBottom = false;
                    }
                }
            }
        }

        private void UpdateAreas()
        {
            Areas.Clear();

            var actualWidth = GetActualWidth();
            var actualHeight = GetActualHeight();

            if (inputService.IsActive
                && actualWidth.HasValue
                && actualHeight.HasValue)
            {
                foreach (var area in inputService.AreaService.Areas)
                {
                    var current = containerProvider.Resolve<AreaViewModel>();

                    current.Initialize(
                        area: area,
                        zoom: zoom,
                        actualLeft: widthMin,
                        actualTop: heightMin,
                        actualWidth: actualWidth,
                        actualHeight: actualHeight,
                        areaService: inputService.AreaService);

                    Areas.Add(current);

                    if (inputService.AreaService.Area == area)
                    {
                        this.area = current;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}