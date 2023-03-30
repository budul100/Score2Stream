using Prism.Regions;
using ScoreboardOCR.Core.Mvvm;

namespace ClipModule.ViewModels
{
    public class ListViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private string _message;

        #endregion Private Fields

        #region Public Constructors

        public ListViewModel(IRegionManager regionManager)
            : base(regionManager)
        {
            Message = "View A from your Prism Module";
        }

        #endregion Public Constructors

        #region Public Properties

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods
    }
}