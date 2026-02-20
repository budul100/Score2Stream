using System;
using Prism.Mvvm;
using Prism.Regions;

namespace Score2Stream.Commons.Prism
{
    public class RegionViewModelBase(IRegionManager regionManager)
        : BindableBase, INavigationAware, IConfirmNavigationRequest
    {
        #region Protected Properties

        protected IRegionManager RegionManager { get; } = regionManager;

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