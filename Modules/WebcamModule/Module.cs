using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using ScoreboardOCR.Core;
using WebcamModule.Views;

namespace WebcamModule
{
    public class Module
        : IModule
    {
        #region Private Fields

        private readonly IRegionManager _regionManager;

        #endregion Private Fields

        #region Public Constructors

        public Module(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        #endregion Public Constructors

        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, nameof(WebcamView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.WebcamView>();
        }

        #endregion Public Methods
    }
}