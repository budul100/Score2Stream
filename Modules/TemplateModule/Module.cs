using Prism.Ioc;
using Prism.Modularity;
using Score2Stream.Core.Enums;

namespace Score2Stream.TemplateModule
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