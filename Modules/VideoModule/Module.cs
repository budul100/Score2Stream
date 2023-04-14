using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Core.Enums;

namespace Score2Stream.VideoModule
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
                regionName: nameof(RegionType.OutputRegion),
                source: nameof(ViewType.Video));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.VideoView>(
                name: nameof(ViewType.Video));

            containerRegistry.Register<ViewModels.SelectionViewModel>();
        }

        #endregion Public Methods
    }
}