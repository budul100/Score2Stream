using ClipModule.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using ScoreboardOCR.Core;

namespace ClipModule
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
            regionManager.RequestNavigate(RegionNames.ClipRegion, nameof(ListView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.ListView>();
        }

        #endregion Public Methods
    }
}