using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Settings;
using Score2Stream.Views;
using Splat;
using System;
using System.Linq;

namespace Score2Stream
{
    public class App
        : PrismApplication
    {
        #region Public Properties

        public static bool IsSingleViewLifetime => Environment
            .GetCommandLineArgs()
            .Any(a => a == "--fbdev" || a == "--drm");

        #endregion Public Properties

        #region Public Methods

        public static AppBuilder BuildAvaloniaApp() => AppBuilder
            .Configure<App>()
            .UsePlatformDetect();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            base.Initialize();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Startup += OnStartup;
            }

            base.OnFrameworkInitializationCompleted();
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<MenuModule.Module>(
                name: nameof(MenuModule));
            moduleCatalog.AddModule<VideoModule.Module>(
                name: nameof(VideoModule));
            moduleCatalog.AddModule<ScoreboardModule.Module>(
                name: nameof(ScoreboardModule));
            moduleCatalog.AddModule<ClipModule.Module>(
                name: nameof(ClipModule));
            moduleCatalog.AddModule<TemplateModule.Module>(
                name: nameof(TemplateModule));
        }

        protected override AvaloniaObject CreateShell()
        {
            //if (IsSingleViewLifetime)
            //    return Container.Resolve<MainControl>(); // For Linux Framebuffer or DRM
            //else
            //    return Container.Resolve<MainWindow>();

            return Container.Resolve<MainView>();
        }

        protected override void OnInitialized()
        {
            var regionManager = Container.Resolve<IRegionManager>();

            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.MenuRegion),
                viewType: typeof(MenuModule.Views.MenuView));

            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.OutputRegion),
                viewType: typeof(VideoModule.Views.VideoView));

            regionManager.RegisterViewWithRegion(
                regionName: nameof(RegionType.EditRegion),
                viewType: typeof(ScoreboardModule.Views.ContentView));
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //// Register Services

            var dispatcherService = new DispatcherService.Service();
            containerRegistry.RegisterInstance<IDispatcherService>(dispatcherService);

            var messageBoxService = new MessageBoxService.Service(App.Current);
            containerRegistry.RegisterInstance<IMessageBoxService>(messageBoxService);

            var recognitionService = new RecognitionService.Service();
            containerRegistry.RegisterInstance<IRecognitionService>(recognitionService);

            containerRegistry.RegisterSingleton<ISettingsService<Session>, SettingsService.Service<Session>>();
            containerRegistry.RegisterSingleton<IScoreboardService, ScoreboardService.Service>();
            containerRegistry.RegisterSingleton<IWebService, WebService.Service>();

            containerRegistry.RegisterSingleton<IInputService, InputService.Service>();
            containerRegistry.Register<IVideoService, VideoService.Service>();
            containerRegistry.Register<IClipService, ClipService.Service>();
            containerRegistry.Register<ITemplateService, TemplateService.Service>();
            containerRegistry.Register<ISampleService, SampleService.Service>();

            // Views - Generic

            containerRegistry.Register<MainView>();
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnStartup(object s, ControlledApplicationLifetimeStartupEventArgs e)
        {
            Control.GotFocusEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
            Control.DoubleTappedEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
        }

        #endregion Private Methods
    }
}