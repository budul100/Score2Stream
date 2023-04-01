using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Core.Constants;

namespace MenuModule
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
                regionName: RegionNames.MenuRegion,
                source: ViewNames.MenuView);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.MenuView>(
                name: ViewNames.MenuView);
        }

        #endregion Public Methods
    }
}