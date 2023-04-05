using Core.Interfaces;
using MvvmDialogs;
using Prism.Ioc;
using Prism.Modularity;
using ScoreboardOCR.Views;
using System.Windows;
using System.Windows.Controls;

namespace ScoreboardOCR
{
    public partial class App
    {
        #region Protected Methods

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<MenuModule.Module>(
                name: nameof(MenuModule));
            moduleCatalog.AddModule<VideoModule.Module>(
                name: nameof(VideoModule));
            moduleCatalog.AddModule<ClipModule.Module>(
                name: nameof(ClipModule));
            moduleCatalog.AddModule<TemplateModule.Module>(
                name: nameof(TemplateModule));
            moduleCatalog.AddModule<ScoreboardModule.Module>(
                name: nameof(ScoreboardModule));
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainView>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(TextBox),
                routedEvent: TextBox.GotFocusEvent,
                handler: new RoutedEventHandler(OnTextBoxFocus));

            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var dispatcherService = new DispatcherService.Service(Current.Dispatcher);
            containerRegistry.RegisterInstance<IDispatcherService>(dispatcherService);

            containerRegistry.RegisterSingleton<IDialogService, DialogService>();

            containerRegistry.RegisterSingleton<IScoreboardService, ScoreboardService.Service>();
            containerRegistry.RegisterSingleton<IGraphicsService, GraphicsService.Service>();

            containerRegistry.RegisterSingleton<IInputService, InputService.Service>();
            containerRegistry.Register<IVideoService, VideoService.Service>();
            containerRegistry.Register<IClipService, ClipService.Service>();
            containerRegistry.Register<ITemplateService, TemplateService.Service>();
            containerRegistry.Register<ISampleService, SampleService.Service>();
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnTextBoxFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.SelectAll();
        }

        #endregion Private Methods
    }
}