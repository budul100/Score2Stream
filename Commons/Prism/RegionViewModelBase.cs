﻿using Prism.Mvvm;
using Prism.Regions;
using System;

namespace Score2Stream.Commons.Prism
{
    public class RegionViewModelBase
        : BindableBase, INavigationAware, IConfirmNavigationRequest
    {
        #region Public Constructors

        public RegionViewModelBase(IRegionManager regionManager)
        {
            RegionManager = regionManager;
        }

        #endregion Public Constructors

        #region Protected Properties

        protected IRegionManager RegionManager { get; }

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