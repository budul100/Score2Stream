using Core.Events;
using Core.Events.Input;
using Core.Interfaces;
using Core.Prism;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace ClipModule.ViewModels
{
    public class ClipsViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IInputService inputService;

        #endregion Private Fields

        #region Public Constructors

        public ClipsViewModel(IInputService inputService, IContainerProvider containerProvider,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.containerProvider = containerProvider;

            eventAggregator
                .GetEvent<InputSelectedEvent>()
                .Subscribe(_ => UpdateClips());

            eventAggregator
                .GetEvent<ClipsChangedEvent>()
                .Subscribe(UpdateClips);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<ClipViewModel> Clips { get; } = new ObservableCollection<ClipViewModel>();

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void UpdateClips()
        {
            Clips.Clear();

            foreach (var clip in inputService.ClipService.Clips)
            {
                var current = containerProvider.Resolve<ClipViewModel>();

                current.Initialize(clip);

                Clips.Add(current);
            }
        }

        #endregion Private Methods
    }
}