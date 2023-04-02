using Core.Constants;
using Prism.Ioc;
using Prism.Modularity;

namespace ScoreboardModule
{
    public class Module
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        { }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.ContentView>(
                name: ViewNames.ContentView);
        }

        #endregion Public Methods
    }
}