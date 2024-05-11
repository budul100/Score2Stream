using Avalonia.Media.Imaging;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.AreaModule.ViewModels
{
    public class SegmentViewModel
        : BindableBase
    {
        #region Private Fields

        private Segment segment;

        #endregion Private Fields

        #region Public Constructors

        public SegmentViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<SegmentUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Description)),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: c => c == segment);

            eventAggregator.GetEvent<SegmentDrawnEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Bitmap)),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: c => c == segment);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => segment.Bitmap;

        public string Description => segment.GetDescription();

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Segment segment)
        {
            this.segment = segment;
        }

        #endregion Public Methods
    }
}