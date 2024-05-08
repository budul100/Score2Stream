using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoModule.ViewModels
{
    public class SegmentViewModel
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

        public SegmentViewModel(INavigationService navigationService, IEventAggregator eventAggregator)
        {
            this.navigationService = navigationService;

            OnPressedCommand = new DelegateCommand(
                executeMethod: () => OnPressed());

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

        public int? Position => Segment?.Position;

        public Segment Segment { get; private set; }

        public double Zoom
        {
            get { return zoom; }
            set { SetProperty(ref zoom, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Segment segment, double zoom, IAreaService areaService)
        {
            this.areaService = areaService;

            this.Segment = segment;
            Zoom = zoom;

            UpdateStatus();
        }

        #endregion Public Methods

        #region Private Methods

        private void OnPressed()
        {
            if (navigationService.EditView == ViewType.Templates)
            {
                areaService.Select(Segment);
            }
        }

        private void UpdateStatus()
        {
            switch (navigationService.EditView)
            {
                case ViewType.Board:

                    IsActive = false;
                    IsSelected = false;

                    break;

                case ViewType.Areas:

                    IsActive = areaService.Area == Segment.Area;
                    IsSelected = false;

                    break;

                case ViewType.Templates:

                    IsActive = areaService.Area == Segment.Area;
                    IsSelected = areaService.Segment == Segment;

                    break;
            }
        }

        #endregion Private Methods
    }
}