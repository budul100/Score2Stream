using Core.Events;
using Prism.Events;
using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Collections.ObjectModel;

namespace ClipModule.ViewModels
{
    public class SelectionViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IClipService clipService;
        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(IClipService clipService, IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.clipService = clipService;
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
                    uniqueNameGetter: (n) => clipService.IsUniqueName(n),
                    eventAggregator: eventAggregator);

                Clips.Add(current);
            }
        }

        #endregion Private Methods
    }
}