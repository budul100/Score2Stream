using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Collections.ObjectModel;

namespace ClipModule.ViewModels
{
    public class ListViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IClipService clipService;

        #endregion Private Fields

        #region Public Constructors

        public ListViewModel(IWebcamService webcamService, IClipService clipService, IRegionManager regionManager)
            : base(regionManager)
        {
            this.clipService = clipService;

            webcamService.OnContentChangedEvent += OnContentChanged;

            clipService.OnClipsChangedEvent += OnClipsChanged;
            clipService.OnClipActivatedEvent += OnClipActivated;
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

        private void OnClipActivated(object sender, System.EventArgs e)
        {
            foreach (var clip in Clips)
            {
                clip.IsActive = clip.Clip == clipService.Active;
            }
        }

        private void OnClipsChanged(object sender, System.EventArgs e)
        {
            SetClips();
        }

        private void OnContentChanged(object sender, System.EventArgs e)
        {
            foreach (var clip in Clips)
            {
                clip.Update();
            }
        }

        private void SetClips()
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

        #endregion Private Methods
    }
}