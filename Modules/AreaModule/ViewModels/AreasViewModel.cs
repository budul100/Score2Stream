using System;
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

        private readonly Func<AreaViewModel> areaViewModelFactory;
        private readonly IInputService inputService;
        private readonly ITemplateService templateService;

        #endregion Private Fields

        #region Public Constructors

        public AreasViewModel(IInputService inputService, ITemplateService templateService,
            Func<AreaViewModel> areaViewModelGetter, IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.templateService = templateService;
            this.areaViewModelFactory = areaViewModelGetter;

            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: _ => UpdateAreas(),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => UpdateAreas(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<AreasChangedEvent>().Subscribe(
                action: UpdateAreas,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<AreasOrderedEvent>().Subscribe(
                action: OrderAreas,
                keepSubscriberReferenceAlive: true);

            UpdateAreas();
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<AreaViewModel> Areas { get; private set; } = [];

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void OrderAreas()
        {
            Areas = new ObservableCollection<AreaViewModel>(Areas.OrderBy(a => a.Area.Index));

            RaisePropertyChanged(nameof(Areas));
        }

        private void UpdateAreas()
        {
            var currents = inputService?.AreaService?.Areas?.ToArray();

            var toBeRemoveds = Areas
                .Where(v => currents?.Contains(v.Area) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Areas.Remove(toBeRemoved);
            }

            if (inputService.AreaService?.Areas?.Count > 0)
            {
                var toBeAddeds = currents
                    .Where(a => !Areas.Any(v => v.Area == a)).ToArray();

                foreach (var toBeAdded in toBeAddeds)
                {
                    var current = areaViewModelFactory.Invoke();

                    current.Initialize(
                        area: toBeAdded,
                        areaService: inputService.AreaService,
                        templateService: templateService);

                    Areas.Add(current);
                }
            }
        }

        #endregion Private Methods
    }
}