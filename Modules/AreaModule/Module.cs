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
            regionManager.RequestNavigate(
                regionName: nameof(RegionType.EditRegion),
                source: nameof(ViewType.Areas));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<AreasView>(
                name: nameof(ViewType.Areas));
        }

        #endregion Public Methods
    }
}