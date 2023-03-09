using Prism.Ioc;
using Prism.Modularity;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Views;
using System.Windows;

namespace ScoreboardOCR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        #region Protected Methods

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<WebcamModule.Module>();
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var dispatcherService = new DispatcherService.Service(Current.Dispatcher);
            containerRegistry.RegisterInstance<IDispatcherService>(dispatcherService);

            containerRegistry.RegisterSingleton<IWebcamService, WebcamService.Service>();
        }

        #endregion Protected Methods
    }
}