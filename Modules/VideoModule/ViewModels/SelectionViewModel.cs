using Core.Events;
using Core.Models;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace VideoModule.ViewModels
{
    public class SelectionViewModel
        : BindableBase, INavigationAware
    {
        #region Private Fields

        private double? height;
        private bool isActive;
        private double? left;
        private double? top;
        private double? width;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Name)),
                keepSubscriberReferenceAlive: true);

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

        public void Initialize(Clip clip, double? actualLeft, double? actualTop, double? actualWidth,
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

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            throw new System.NotImplementedException();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            throw new System.NotImplementedException();
        }

        #endregion Public Methods
    }
}