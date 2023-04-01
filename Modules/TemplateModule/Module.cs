using Prism.Ioc;
using Prism.Modularity;
using Core.Constants;

namespace TemplateModule
{
    public class Module
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        { }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.SelectionView>(
                name: ViewNames.TemplateView);
        }

        #endregion Public Methods
    }
}