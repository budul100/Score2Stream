using Core.Events;
using Core.Interfaces;
using Core.Mvvm;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace ClipModule.ViewModels
{
    public class SelectionViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IClipService clipService;
        private readonly IInputService inputService;
        private readonly IEventAggregator eventAggregator;
        private readonly ITemplateService templateService;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(IInputService inputService, IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.eventAggregator = eventAggregator;

            eventAggregator
                .GetEvent<ClipsChangedEvent>()
                .Subscribe(SetClips);
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

        private void SetClips()
        {
            Clips.Clear();

            foreach (var clip in clipService.Clips)
            {
                var current = new ClipViewModel(
                    clip: clip,
                    clipService: clipService,
                    templateService: templateService,
                    eventAggregator: eventAggregator);

                Clips.Add(current);
            }
        }

        #endregion Private Methods
    }
}