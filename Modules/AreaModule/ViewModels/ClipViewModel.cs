using Avalonia.Media.Imaging;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.AreaModule.ViewModels
{
    public class ClipViewModel
        : BindableBase
    {
        #region Private Fields

        private Segment clip;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Description)),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: c => c == clip);

            eventAggregator.GetEvent<ClipDrawnEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Bitmap)),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: c => c == clip);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => clip.Bitmap;

        public string Description => clip.GetDescription();

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Segment clip)
        {
            this.clip = clip;
        }

        #endregion Public Methods
    }
}