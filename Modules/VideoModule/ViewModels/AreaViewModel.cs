using System;
using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Segment;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoModule.ViewModels
{
    public class AreaViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly INavigationService navigationService;
        private readonly Func<SegmentViewModel> segmentViewModelFactory;

        private IAreaService areaService;
        private double? height;
        private double? heightName;
        private bool isActive;
        private double? left;
        private double? top;
        private double? width;
        private double? widthName;
        private double zoom;

        #endregion Private Fields

        #region Public Constructors

        public AreaViewModel(Func<SegmentViewModel> segmentViewModelFactory, INavigationService navigationService,
            IEventAggregator eventAggregator)
        {
            this.segmentViewModelFactory = segmentViewModelFactory;
            this.navigationService = navigationService;

            OnPressedCommand = new DelegateCommand(
                executeMethod: OnPressed);

            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: _ => UpdateStatus(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SegmentSelectedEvent>().Subscribe(
                action: _ => UpdateStatus(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TabSelectedEvent>().Subscribe(
                action: _ => UpdateStatus(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Area Area { get; private set; }

        public double? Bottom => HasValue
            ? Top.Value + Height
            : default;

        public string Description => HasValue && !IsEditing && IsVisible
            ? Area?.Description
            : default;

        public bool HasValue => left.HasValue || top.HasValue;

        public double? Height
        {
            get
            {
                return HasValue
                    ? height
                    : default;
            }
            set
            {
                var current = !double.IsNaN(value ?? double.NaN)
                    ? value
                    : default;

                SetProperty(ref height, current);
            }
        }

        public double? HeightName
        {
            get
            {
                return HasValue && IsVisible
                    ? heightName
                    : default;
            }
            set
            {
                var current = !double.IsNaN(value ?? double.NaN)
                    ? value
                    : default;

                SetProperty(ref heightName, current);
            }
        }

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                SetProperty(ref isActive, value);
            }
        }

        public bool IsEditing { get; set; }

        public bool IsVisible => (Height == default && Width == default)
            || (Height >= Constants.SizeChangeMin && Width >= Constants.SizeChangeMin);

        public double? Left
        {
            get
            {
                return HasValue
                    ? left
                    : default;
            }
            set
            {
                var current = !double.IsNaN(value ?? double.NaN)
                    ? value
                    : default;

                SetProperty(ref left, current);
            }
        }

        public DelegateCommand OnPressedCommand { get; }

        public double? Right => HasValue
            ? Left.Value + Width
            : default;

        public ObservableCollection<SegmentViewModel> Segments { get; } = [];

        public int Size => Segments.Count;

        public double? Top
        {
            get
            {
                return HasValue
                    ? top
                    : default;
            }
            set
            {
                var current = !double.IsNaN(value ?? double.NaN)
                    ? value
                    : default;

                SetProperty(ref top, current);
            }
        }

        public double? Width
        {
            get
            {
                return HasValue
                    ? width
                    : default;
            }
            set
            {
                var current = !double.IsNaN(value ?? double.NaN)
                    ? value
                    : default;

                SetProperty(ref width, current);
            }
        }

        public double? WidthName
        {
            get
            {
                return HasValue && IsVisible
                    ? widthName
                    : default;
            }
            set
            {
                var current = !double.IsNaN(value ?? double.NaN)
                    ? value
                    : default;

                SetProperty(ref widthName, current);
            }
        }

        public double Zoom
        {
            get
            {
                return zoom > 0
                    ? zoom
                    : 1;
            }
            set
            {
                if (zoom != value)
                {
                    SetProperty(ref zoom, value);

                    foreach (var segment in Segments)
                    {
                        segment.Zoom = zoom;
                    }
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Area area, double zoom, double? actualLeft, double? actualTop,
            double? actualWidth, double? actualHeight, IAreaService areaService)
        {
            this.areaService = areaService;

            this.Area = area;
            Zoom = zoom;

            RaisePropertyChanged(nameof(Description));

            InitializeSegments(areaService);

            InitializeSize(
                actualLeft: actualLeft,
                actualTop: actualTop,
                actualWidth: actualWidth,
                actualHeight: actualHeight);

            UpdateStatus();
        }

        #endregion Public Methods

        #region Private Methods

        private void InitializeSegments(IAreaService areaService)
        {
            Segments.Clear();

            foreach (var segment in Area.Segments)
            {
                var viewModel = segmentViewModelFactory.Invoke();

                viewModel.Initialize(
                    segment: segment,
                    zoom: zoom,
                    areaService: areaService);

                Segments.Add(viewModel);
            }

            RaisePropertyChanged(nameof(Size));
            RaisePropertyChanged(nameof(Segments));
        }

        private void InitializeSize(double? actualLeft, double? actualTop, double? actualWidth,
            double? actualHeight)
        {
            if (actualWidth.HasValue
                && actualHeight.HasValue)
            {
                Width = (Area.X2 - Area.X1) * actualWidth;
                Height = (Area.Y2 - Area.Y1) * actualHeight;

                Left = actualLeft + (Area.X1 * actualWidth);
                Top = actualTop + (Area.Y1 * actualHeight);
            }
        }

        private void OnPressed()
        {
            if (navigationService.EditView == ViewType.Inputs)
            {
                areaService.Select(Area);
            }
        }

        private void UpdateStatus()
        {
            IsActive = navigationService.EditView != ViewType.Board
                && areaService.Active == Area;
        }

        #endregion Private Methods
    }
}