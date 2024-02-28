using System.Collections.ObjectModel;
using System.Linq;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;

namespace Score2Stream.AreaModule.ViewModels
{
    public class AreasViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IInputService inputService;

        #endregion Private Fields

        #region Public Constructors

        public AreasViewModel(IInputService inputService, IContainerProvider containerProvider,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.containerProvider = containerProvider;

            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => UpdateClips(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<InputsChangedEvent>().Subscribe(
                action: UpdateClips,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: UpdateClips,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreasOrderedEvent>().Subscribe(
                action: () => OrderAreas(),
                keepSubscriberReferenceAlive: true);

            UpdateClips();
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<AreaViewModel> Areas { get; private set; } = new ObservableCollection<AreaViewModel>();

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void OrderAreas()
        {
            Areas = new ObservableCollection<AreaViewModel>(Areas.OrderBy(a => a.Index));

            RaisePropertyChanged(nameof(Areas));
        }

        private void UpdateClips()
        {
            Areas.Clear();

            if (inputService.AreaService?.Areas?.Any() == true)
            {
                foreach (var area in inputService.AreaService.Areas)
                {
                    var current = containerProvider.Resolve<AreaViewModel>();

                    current.Initialize(
                        area: area,
                        areaService: inputService.AreaService);

                    Areas.Add(current);
                }

                OrderAreas();
            }
        }

        #endregion Private Methods
    }
}