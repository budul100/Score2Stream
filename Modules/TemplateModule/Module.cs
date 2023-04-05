using Core.Enums;
using Prism.Ioc;
using Prism.Modularity;

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
            containerRegistry.RegisterForNavigation<Views.TemplateView>(
                name: nameof(ViewType.Templates));
        }

        #endregion Public Methods
    }
}