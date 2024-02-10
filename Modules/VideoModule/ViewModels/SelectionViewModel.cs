using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoModule.ViewModels
{
    public class SelectionViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly INavigationService navigationService;

        private IClipService clipService;
        private double? height;
        private double? heightName;
        private bool isActive;
        private bool isEditing;
        private double? left;
        private double? top;
        private double? width;
        private double? widthName;
        private double zoom;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(INavigationService navigationService, IEventAggregator eventAggregator)
        {
            this.navigationService = navigationService;

            OnSelectionCommand = new DelegateCommand(
                executeMethod: () => OnSelection());

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => IsActive = c == Clip,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public double? Bottom => HasValue
            ? Top.Value + Height
            : default;

        public Clip Clip { get; private set; }

        public string Description => HasValue && !IsEditing && Width > 0 && Height > 0
            ? Clip?.Description
            : default;

        public double FontSize => Constants.SelectionFontSize / Zoom;

        public bool HasValue => Left.HasValue && Top.HasValue;

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
                return HasValue
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

                RaisePropertyChanged(nameof(Thickness));
                RaisePropertyChanged(nameof(FontSize));
            }
        }

        public bool IsEditing
        {
            get { return isEditing; }
            set
            {
                SetProperty(ref isEditing, value);

                RaisePropertyChanged(nameof(Description));
            }
        }

        public double? Left
        {
            get { return left; }
            set
            {
                var current = !double.IsNaN(value ?? double.NaN)
                    ? value
                    : default;

                SetProperty(ref left, current);
            }
        }

        public DelegateCommand OnSelectionCommand { get; }

        public double? Right => HasValue
            ? Left.Value + Width
            : default;

        public double Thickness => IsActive
            ? Constants.SelectionThicknessActive / Zoom
            : Constants.SelectionThicknessNormal / Zoom;

        public double? Top
        {
            get { return top; }
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
                return HasValue
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
                    zoom = value;

                    RaisePropertyChanged(nameof(Thickness));
                    RaisePropertyChanged(nameof(FontSize));
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Clip clip, double zoom, double? actualLeft, double? actualTop, double? actualWidth,
            double? actualHeight, IClipService clipService)
        {
            this.clipService = clipService;

            Clip = clip;
            Zoom = zoom;

            IsActive = clipService.Active == clip;

            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Thickness));
            RaisePropertyChanged(nameof(FontSize));

            if (actualWidth.HasValue
                && actualHeight.HasValue)
            {
                Left = actualLeft + (Clip.X1 * actualWidth);
                Width = (Clip.X2 - Clip.X1) * actualWidth;
                Top = actualTop + (Clip.Y1 * actualHeight);
                Height = (Clip.Y2 - Clip.Y1) * actualHeight;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void OnSelection()
        {
            if (navigationService.EditView != ViewType.Clips)
            {
                clipService.Select(Clip);
            }
        }

        #endregion Private Methods
    }
}