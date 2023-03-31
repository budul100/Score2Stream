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
        private readonly ITemplateService templateService;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(IClipService clipService, ITemplateService templateService,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.clipService = clipService;
            this.templateService = templateService;
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