using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;

namespace WebcamModule.ViewModels
{
    public class ClipViewModel
        : ViewModelBase
    {
        #region Private Fields

        private double? height;
        private bool isActive;
        private double? left;
        private double? top;
        private double? width;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(Clip clip, double? actualLeft, double? actualTop, double? actualWidth,
            double? actualHeight)
        {
            Clip = clip;

            if (actualWidth.HasValue
                && actualHeight.HasValue)
            {
                Left = actualLeft + (Clip.RelativeX1 * actualWidth);
                Width = (Clip.RelativeX2 - Clip.RelativeX1) * actualWidth;
                Top = actualTop + (Clip.RelativeY1 * actualHeight);
                Height = (Clip.RelativeY2 - Clip.RelativeY1) * actualHeight;
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public double? Bottom => HasValue
            ? Top.Value + Height
            : default;

        public Clip Clip { get; }

        public bool HasValue => Left.HasValue
            && Top.HasValue;

        public double? Height
        {
            get { return height; }
            set
            {
                SetProperty(ref height, value);
                RaisePropertyChanged(nameof(Name));
            }
        }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public double? Left
        {
            get { return left; }
            set { SetProperty(ref left, value); }
        }

        public string Name => HasValue && (Width > 0 || Height > 0)
            ? Clip.Name
            : default;

        public double? Right => HasValue
            ? Left.Value + Width
            : default;

        public double? Top
        {
            get { return top; }
            set { SetProperty(ref top, value); }
        }

        public double? Width
        {
            get { return width; }
            set
            {
                SetProperty(ref width, value);
                RaisePropertyChanged(nameof(Name));
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Update()
        {
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(Name));
        }

        #endregion Public Methods
    }
}