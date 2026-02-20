using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Commons.Enums;
using Score2Stream.MenuModule.Views;

namespace Score2Stream.MenuModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion<MenuView>(
                regionName: nameof(RegionType.MenuRegion));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        { }

        #endregion Public Methods
    }
}