﻿using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Core.Enums;

namespace Score2Stream.MenuModule
{
    public class Module
        : IModule
    {
        #region Private Fields

        private readonly IRegionManager regionManager;

        #endregion Private Fields

        #region Public Constructors

        public Module(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        #endregion Public Constructors

        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RequestNavigate(
                regionName: nameof(RegionType.MenuRegion),
                source: nameof(ViewType.Menu));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.MenuView>(
                name: nameof(ViewType.Menu));
        }

        #endregion Public Methods
    }
}