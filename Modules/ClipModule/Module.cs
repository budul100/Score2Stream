using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Commons.Enums;

namespace Score2Stream.ClipModule
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
                source: nameof(ViewType.Clips));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.ClipsView>(
                name: nameof(ViewType.Clips));

            containerRegistry.RegisterForNavigation<Views.ClipView>(
                name: typeof(Views.ClipView).FullName);
        }

        #endregion Public Methods
    }
}