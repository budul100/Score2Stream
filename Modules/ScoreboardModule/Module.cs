using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Commons.Enums;
using Score2Stream.ScoreboardModule.Views;

namespace Score2Stream.ScoreboardModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion<ContentView>(
                regionName: nameof(RegionType.EditRegion));

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