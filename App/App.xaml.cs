﻿using Prism.Ioc;
using Prism.Modularity;
using Core.Interfaces;
using ScoreboardOCR.Views;
using System.Windows;

namespace ScoreboardOCR
{
    public partial class App
    {
        #region Protected Methods

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<MenuModule.Module>(
                name: nameof(MenuModule));
            moduleCatalog.AddModule<WebcamModule.Module>(
                name: nameof(WebcamModule));
            moduleCatalog.AddModule<ClipModule.Module>(
                name: nameof(ClipModule));
            moduleCatalog.AddModule<TemplateModule.Module>(
                name: nameof(TemplateModule));
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
            containerRegistry.RegisterSingleton<IClipService, ClipService.Service>();
            containerRegistry.RegisterSingleton<ITemplateService, TemplateService.Service>();
        }

        #endregion Protected Methods
    }
}