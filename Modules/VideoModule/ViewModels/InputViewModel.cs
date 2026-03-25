using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoModule.ViewModels
{
    public class InputViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly Func<AreaViewModel> areaViewModelFactory;
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
        private bool isDragging;
        private bool isLoading;
        private bool isMouseDown;
        private double mouseDownX;
        private double mouseDownY;
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

        public InputViewModel(IInputService inputService, Func<AreaViewModel> areaViewModelFactory,
            IDialogService dialogService, INavigationService navigationService,
            IEventAggregator eventAggregator)
        {
            this.inputService = inputService;
            this.navigationService = navigationService;
            this.dialogService = dialogService;
            this.areaViewModelFactory = areaViewModelFactory;

            MousePressedCommand = new DelegateCommand<PointerPressedEventArgs>(OnMousePressed);
            MouseReleasedCommand = new DelegateCommand<PointerReleasedEventArgs>(OnMouseReleasedAsync);
            ZoomChangedCommand = new DelegateCommand<ZoomChangedEventArgs>(OnZoomChanged);

            eventAggregator.GetEvent<InputDrawnEvent>().Subscribe(
                action: RefreshInput,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<InputCenteringEvent>().Subscribe(
                action: CenterInput,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: SelectArea,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: RefreshAreas,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => RefreshAreas(),
                keepSubscriberReferenceAlive: true);

            IsLoading = true;
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler CenterInputEvent;

        #endregion Public Events

        #region Public Properties

        public static double ZoomMax => Constants.ZoomMax;

        public static double ZoomMin => Constants.ZoomMin;

        public ObservableCollection<AreaViewModel> Areas { get; } = [];

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

        public DelegateCommand CloseCommand { get; private set; }

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

        public Input Input { get; private set; }

        public bool IsLoading
        {
            get { return isLoading; }
            set { SetProperty(ref isLoading, value); }
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

                RefreshArea();
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

                RefreshArea();
            }
        }

        public string Name => Input?.Name;

        public DelegateCommand<ZoomChangedEventArgs> ZoomChangedCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Input input)
        {
            Input = input;

            CloseCommand = new DelegateCommand(async () => await RemoveAsync());

            RaisePropertyChanged(nameof(Name));
        }

        #endregion Public Methods

        #region Private Methods

        private void CenterInput()
        {
            CenterInputEvent?.Invoke(
                sender: this,
                e: default);
        }

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
            var result = navigationService.EditView == ViewType.Inputs
                && Bitmap != default
                && area != default
                && (area.IsEditing || isActivating);

            return result;
        }

        private void OnMousePressed(PointerPressedEventArgs eventArgs)
        {
            var pointerUpdateKind = eventArgs.GetCurrentPoint(default)
                .Properties.PointerUpdateKind;

            if (pointerUpdateKind == PointerUpdateKind.LeftButtonPressed
                && Bitmap != default
                && area != default)
            {
                isMouseDown = true;
                isDragging = false;
                mouseDownX = mouseX;
                mouseDownY = mouseY;
            }
        }

        private async void OnMouseReleasedAsync(PointerReleasedEventArgs eventArgs)
        {
            var pointerUpdateKind = eventArgs.GetCurrentPoint(default)
                .Properties.PointerUpdateKind;

            if (pointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                if (isDragging
                    && IsMouseEditing())
                {
                    var isResized = false;

                    if (area.IsVisible)
                    {
                        var canBeResized = !area.Area.HasDimensions;

                        if (!canBeResized)
                        {
                            var messageBoxResult = await dialogService.GetMessageBoxResultAsync(
                                contentMessage: "Shall the dimensions of the area be changed?",
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
                else if (isMouseDown
                    && !isDragging)
                {
                    var clickedInsideArea = Areas.Any(a => a.HasValue
                        && mouseDownX >= (a.Left ?? 0)
                        && mouseDownX <= (a.Right ?? 0)
                        && mouseDownY >= (a.Top ?? 0)
                        && mouseDownY <= (a.Bottom ?? 0));

                    if (!clickedInsideArea)
                    {
                        inputService.AreaService.Select(
                            area: default);
                    }
                }

                isMouseDown = false;
                isDragging = false;
            }
        }

        private void OnZoomChanged(ZoomChangedEventArgs eventArgs)
        {
            zoom = eventArgs.ZoomX;

            foreach (var area in Areas)
            {
                area.Zoom = zoom;
            }
        }

        private void RefreshArea()
        {
            if (!isMouseDown || !IsMouseEditing(isActivating: !isDragging))
            {
                return;
            }

            if (!isDragging)
            {
                var dx = Math.Abs(mouseX - mouseDownX);
                var dy = Math.Abs(mouseY - mouseDownY);

                if (dx < Constants.DragThreshold
                    && dy < Constants.DragThreshold)
                {
                    return;
                }

                isDragging = true;
                area.IsEditing = true;
                area.Left = mouseDownX;
                area.Top = mouseDownY;
                area.Height = default;
                area.Width = default;
            }

            if (!area.HasValue)
            {
                area.Left = mouseX;
                area.Top = mouseY;
            }
            else
            {
                if (mouseX > (area.Right ?? 0) || (mouseX >= (area.Left ?? 0)
                    && movedToRight))
                {
                    area.Width = mouseX - area.Left.Value;
                    movedToRight = true;
                }
                else if (mouseX < (area.Left ?? 0) || (mouseX <= (area.Right ?? 0)
                    && !movedToRight))
                {
                    area.Width = (area.Width ?? 0) + area.Left.Value - mouseX;
                    area.Left = mouseX;
                    movedToRight = false;
                }

                if (mouseY > (area.Bottom ?? 0) || (mouseY >= (area.Top ?? 0)
                    && movedToBottom))
                {
                    area.Height = mouseY - area.Top.Value;
                    movedToBottom = true;
                }
                else if (mouseY < (area.Top ?? 0) || (mouseY <= (area.Bottom ?? 0)
                    && !movedToBottom))
                {
                    area.Height = (area.Height ?? 0) + area.Top.Value - mouseY;
                    area.Top = mouseY;
                    movedToBottom = false;
                }
            }
        }

        private void RefreshAreas()
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
                    var current = areaViewModelFactory.Invoke();

                    current.Initialize(
                        area: area,
                        zoom: zoom,
                        actualLeft: widthMin,
                        actualTop: heightMin,
                        actualWidth: actualWidth,
                        actualHeight: actualHeight,
                        areaService: inputService.AreaService);

                    Areas.Add(current);

                    if (inputService.AreaService.Active == area)
                    {
                        this.area = current;
                    }
                }
            }
        }

        private void RefreshInput()
        {
            Bitmap = inputService.VideoService?.Bitmap;

            if (Bitmap != default
                && IsLoading)
            {
                RefreshAreas();

                IsLoading = false;
            }
        }

        private async Task RemoveAsync()
        {
            if (Input == default) return;

            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: $"Shall {Name} be removed?",
                contentTitle: "Remove input");

            if (result == ButtonResult.Yes)
            {
                await inputService.RemoveAsync(Input);
            }
        }

        private void SelectArea(Area area)
        {
            this.area = Areas
                .SingleOrDefault(a => area == a.Area);
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

            RefreshAreas();
        }

        #endregion Private Methods
    }
}