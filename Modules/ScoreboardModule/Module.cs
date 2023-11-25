using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Commons.Enums;
using Score2Stream.ScoreboardModule.Views;

namespace Score2Stream.ScoreboardModule
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
                regionName: nameof(RegionType.EditRegion),
                source: nameof(ViewType.Board));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ContentView>(
                name: nameof(ViewType.Board));
        }

        #endregion Public Methods
    }
}