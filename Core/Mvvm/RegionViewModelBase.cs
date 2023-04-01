using Prism.Regions;
using System;

namespace Core.Mvvm
{
    public class RegionViewModelBase
        : ViewModelBase, INavigationAware, IConfirmNavigationRequest
    {
        #region Public Constructors

        public RegionViewModelBase(IRegionManager regionManager)
        {
            RegionManager = regionManager;
        }

        #endregion Public Constructors

        #region Protected Properties

        protected IRegionManager RegionManager { get; private set; }

        #endregion Protected Properties

        #region Public Methods

        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            continuationCallback(true);
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        { }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods
    }
}