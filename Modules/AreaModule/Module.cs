using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.AreaModule.Views;
using Score2Stream.Commons.Enums;

namespace Score2Stream.AreaModule
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
                source: nameof(ViewType.Areas));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<AreasView>(
                name: nameof(ViewType.Areas));

            containerRegistry.RegisterForNavigation<AreaView>();
        }

        #endregion Public Methods
    }
}