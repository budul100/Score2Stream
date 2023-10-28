using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Core;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Models.Contents;

namespace Score2Stream.VideoModule.ViewModels
{
    public class SelectionViewModel
        : BindableBase
    {
        #region Private Fields

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

        public SelectionViewModel(IEventAggregator eventAggregator)
        {
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

        public void Initialize(Clip clip, bool isActive, double zoom, double? actualLeft, double? actualTop,
            double? actualWidth, double? actualHeight)
        {
            Clip = clip;
            IsActive = isActive;
            Zoom = zoom;

            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Thickness));
            RaisePropertyChanged(nameof(FontSize));

            if (actualWidth.HasValue
                && actualHeight.HasValue)
            {
                Left = actualLeft + (Clip.RelativeX1 * actualWidth);
                Width = (Clip.RelativeX2 - Clip.RelativeX1) * actualWidth;
                Top = actualTop + (Clip.RelativeY1 * actualHeight);
                Height = (Clip.RelativeY2 - Clip.RelativeY1) * actualHeight;
            }
        }

        #endregion Public Methods
    }
}