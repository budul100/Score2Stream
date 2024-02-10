using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Score2Stream.App.Views;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Splat;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Score2Stream.App
{
    public class App
        : PrismApplication
    {
        #region Private Fields

        private IClassicDesktopStyleApplicationLifetime desktop;
        private MainView mainWindow;
        private SplashView splashWindow;

        #endregion Private Fields

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
            desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

            if (desktop != default)
            {
                desktop.Startup += OnStartup;

                splashWindow = Container.Resolve<SplashView>();

                desktop.MainWindow = splashWindow;
                splashWindow.Show();

                var dispatcherService = Container.Resolve<IDispatcherService>();
                Task.Run(() => dispatcherService.InvokeAsync(InitializeApp));
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
            mainWindow = !IsSingleViewLifetime
                ? Container.Resolve<MainView>()
                : default; // For Linux Framebuffer or DRM: Container.Resolve<MainControl>();

            return mainWindow;
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
            var dispatcherService = new DispatcherService.Service();
            containerRegistry.RegisterInstance<IDispatcherService>(dispatcherService);

            var recognitionService = new RecognitionService.Service();
            containerRegistry.RegisterInstance<IRecognitionService>(recognitionService);

            containerRegistry.RegisterSingleton<IDialogService, DialogService.Service>();
            containerRegistry.RegisterSingleton<INavigationService, NavigationService.Service>();
            containerRegistry.RegisterSingleton<ISettingsService<Session>, SettingsService.Service<Session>>();
            containerRegistry.RegisterSingleton<IScoreboardService, ScoreboardService.Service>();
            containerRegistry.RegisterSingleton<IWebService, WebService.Service>();

            containerRegistry.RegisterSingleton<IInputService, InputService.Service>();
            containerRegistry.Register<IVideoService, VideoService.Service>();
            containerRegistry.Register<IClipService, ClipService.Service>();
            containerRegistry.Register<ITemplateService, TemplateService.Service>();
            containerRegistry.Register<ISampleService, SampleService.Service>();

            containerRegistry.Register<MainView>();
        }

        #endregion Protected Methods

        #region Private Methods

        private void InitializeApp()
        {
            try
            {
                var inputService = Container.Resolve<IInputService>();
                inputService.Initialize();

                desktop.MainWindow = mainWindow;

                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                var iconUri = $"avares://{assemblyName}/Assets/{assemblyName}.png";

                var dialogService = Container.Resolve<IDialogService>();
                dialogService.Initialize(
                    window: mainWindow,
                    iconUri: iconUri);

                splashWindow.Close();
            }
            catch (TaskCanceledException)
            {
                desktop.MainWindow = default;

                splashWindow.Close();
            }
        }

        private void OnStartup(object s, ControlledApplicationLifetimeStartupEventArgs e)
        {
            Control.GotFocusEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
            Control.DoubleTappedEvent.AddClassHandler<TextBox>((s, _) => s.SelectAll());
        }

        #endregion Private Methods
    }
}