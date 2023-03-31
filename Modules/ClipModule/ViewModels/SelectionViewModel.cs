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

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(IWebcamService webcamService, IClipService clipService, IRegionManager regionManager)
            : base(regionManager)
        {
            this.clipService = clipService;

            webcamService.OnContentUpdatedEvent += OnContentUpdated;

            clipService.OnClipsChangedEvent += OnClipsChanged;
            clipService.OnClipsUpdatedEvent += OnClipsUpdated;
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

        private void OnClipsChanged(object sender, System.EventArgs e)
        {
            Clips.Clear();

            foreach (var clip in clipService.Clips)
            {
                var current = new ClipViewModel(
                    clipService: clipService,
                    clip: clip);

                Clips.Add(current);
            }
        }

        private void OnClipsUpdated(object sender, System.EventArgs e)
        {
            UpdateClips();
        }

        private void OnContentUpdated(object sender, System.EventArgs e)
        {
            UpdateClips();
        }

        private void UpdateClips()
        {
            foreach (var clip in Clips)
            {
                clip.Update(clipService.Selection);
            }
        }

        #endregion Private Methods
    }
}