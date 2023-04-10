using Core.Events.Clip;
using Core.Models;
using Prism.Events;
using Prism.Mvvm;

namespace VideoModule.ViewModels
{
    public class SelectionViewModel
        : BindableBase
    {
        #region Private Fields

        private double? height;
        private double? heightName;
        private bool isActive;
        private double? left;
        private double? top;
        private double? width;
        private double? widthName;

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

        public string Description => HasValue && Width > 0 && Height > 0
            ? Clip?.Description
            : default;

        public bool HasValue => Left.HasValue && Top.HasValue;

        public double? Height
        {
            get { return height; }
            set
            {
                SetProperty(ref height, value);
            }
        }

        public double? HeightName
        {
            get { return heightName; }
            set { SetProperty(ref heightName, value); }
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
            }
        }

        public double? WidthName
        {
            get { return widthName; }
            set { SetProperty(ref widthName, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Clip clip, bool isActive, double? actualLeft, double? actualTop, double? actualWidth,
            double? actualHeight)
        {
            Clip = clip;
            IsActive = isActive;

            RaisePropertyChanged(nameof(Description));

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