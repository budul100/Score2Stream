using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.AreaModule.Views;
using Score2Stream.Commons.Enums;

namespace Score2Stream.AreaModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion<AreasView>(
                regionName: nameof(RegionType.EditRegion));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<AreasView>(
                name: nameof(ViewType.Inputs));
        }

        #endregion Public Methods
    }
}