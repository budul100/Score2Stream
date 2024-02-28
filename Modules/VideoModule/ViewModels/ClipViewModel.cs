using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoModule.ViewModels
{
    public class ClipViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly INavigationService navigationService;

        private IAreaService areaService;
        private bool isActive;
        private bool isSelected;

        private double zoom;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(INavigationService navigationService, IEventAggregator eventAggregator)
        {
            this.navigationService = navigationService;

            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: a => OnAreaSelected(a),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => IsSelected = c == Clip,
                keepSubscriberReferenceAlive: true);

            OnPressedCommand = new DelegateCommand(
                executeMethod: () => OnPressed());
        }

        #endregion Public Constructors

        #region Public Properties

        public Clip Clip { get; private set; }

        public int? Index => Clip?.Index;

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                SetProperty(ref isActive, value);
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                SetProperty(ref isSelected, value);
            }
        }

        public DelegateCommand OnPressedCommand { get; }

        public double Zoom
        {
            get { return zoom; }
            set { SetProperty(ref zoom, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Clip clip, double zoom, IAreaService areaService)
        {
            this.areaService = areaService;

            Clip = clip;
            Zoom = zoom;

            IsActive = areaService.Area == Clip.Area;
        }

        #endregion Public Methods

        #region Private Methods

        private void OnAreaSelected(Area area)
        {
            IsActive = area == Clip?.Area;

            if (!IsActive)
            {
                IsSelected = false;
            }
        }

        private void OnPressed()
        {
            if (navigationService.EditView != ViewType.Areas)
            {
                areaService.Select(Clip);
            }
        }

        #endregion Private Methods
    }
}