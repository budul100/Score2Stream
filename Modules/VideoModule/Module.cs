using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Commons.Enums;
using Score2Stream.VideoModule.Views;

namespace Score2Stream.VideoModule
{
    public class Module(IRegionManager regionManager)
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RegisterViewWithRegion<InputView>(
                regionName: nameof(RegionType.OutputRegion));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        { }

        #endregion Public Methods
    }
}