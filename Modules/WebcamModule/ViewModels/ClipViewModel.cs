using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;

namespace WebcamModule.ViewModels
{
    public class ClipViewModel
        : ViewModelBase
    {
        #region Private Fields

        private double? height;
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
            set { SetProperty(ref height, value); }
        }

        public double? Left
        {
            get { return left; }
            set { SetProperty(ref left, value); }
        }

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
            set { SetProperty(ref width, value); }
        }

        #endregion Public Properties
    }
}