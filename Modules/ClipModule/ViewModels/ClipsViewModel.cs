using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Input;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Prism;
using System.Collections.ObjectModel;
using System.Linq;

namespace Score2Stream.ClipModule.ViewModels
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
                .GetEvent<InputsChangedEvent>()
                .Subscribe(UpdateClips);

            eventAggregator
                .GetEvent<ClipsChangedEvent>()
                .Subscribe(UpdateClips);
            eventAggregator.GetEvent<ClipsOrderedEvent>().Subscribe(
                action: () => OrderClips(),
                keepSubscriberReferenceAlive: true);

            UpdateClips();
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<ClipViewModel> Clips { get; private set; } = new ObservableCollection<ClipViewModel>();

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void OrderClips()
        {
            Clips = new ObservableCollection<ClipViewModel>(Clips.OrderBy(s => s.Clip.Index));

            RaisePropertyChanged(nameof(Clips));
        }

        private void UpdateClips()
        {
            Clips.Clear();

            if (inputService.ClipService?.Clips?.Any() == true)
            {
                foreach (var clip in inputService.ClipService.Clips)
                {
                    var current = containerProvider.Resolve<ClipViewModel>();

                    current.Initialize(
                        clip: clip,
                        clipService: inputService.ClipService);

                    Clips.Add(current);
                }

                OrderClips();
            }
        }

        #endregion Private Methods
    }
}