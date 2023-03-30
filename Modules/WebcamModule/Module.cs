using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using ScoreboardOCR.Core.Constants;

namespace WebcamModule
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
                regionName: RegionNames.WebcamRegion,
                source: ViewNames.WebcamView);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.WebcamView>(
                name: ViewNames.WebcamView);
        }

        #endregion Public Methods
    }
}