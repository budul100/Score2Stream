﻿using Prism.Commands;
using Prism.Regions;
using ScoreboardOCR.Core.Constants;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;

namespace MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int IndexClipView = 0;
        private const int IndexTemplateView = 1;

        private readonly IClipService clipService;
        private readonly IRegionManager regionManager;
        private readonly IWebcamService webcamService;

        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IWebcamService webcamService, IClipService clipService, IRegionManager regionManager)
            : base(regionManager)
        {
            this.webcamService = webcamService;
            this.clipService = clipService;
            this.regionManager = regionManager;

            webcamService.OnContentChangedEvent += OnContentChanged;
            clipService.OnClipsChangedEvent += OnClipsChanged;

            this.WebcamPlayCommand = new DelegateCommand(
                executeMethod: WebcamStartAsync,
                canExecuteMethod: () => !webcamService.IsActive);
            this.WebcamPauseCommand = new DelegateCommand(
                executeMethod: WebcamStopAsync,
                canExecuteMethod: () => webcamService.IsActive);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: ClipAdd,
                canExecuteMethod: () => webcamService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: ClipRemove,
                canExecuteMethod: () => clipService.Active != default);
        }

        #endregion Public Constructors

        #region Public Properties

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                SetProperty(ref selectedTabIndex, value);

                switch (SelectedTabIndex)
                {
                    case IndexClipView:
                        regionManager.RequestNavigate(
                            regionName: RegionNames.EditRegion,
                            source: ViewNames.ClipView);
                        break;

                    case IndexTemplateView:
                        regionManager.RequestNavigate(
                            regionName: RegionNames.EditRegion,
                            source: ViewNames.TemplateView);
                        break;
                }
            }
        }

        public DelegateCommand WebcamPauseCommand { get; }

        public DelegateCommand WebcamPlayCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void ClipAdd()
        {
            clipService.Add();
        }

        private void ClipRemove()
        {
            clipService.Remove();
        }

        private void OnClipsChanged(object sender, System.EventArgs e)
        {
            ClipAddCommand.RaiseCanExecuteChanged();
            ClipRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnContentChanged(object sender, System.EventArgs e)
        {
            WebcamPlayCommand.RaiseCanExecuteChanged();
            WebcamPauseCommand.RaiseCanExecuteChanged();

            ClipAddCommand.RaiseCanExecuteChanged();
        }

        private async void WebcamStartAsync()
        {
            await webcamService.StartAsync();
        }

        private async void WebcamStopAsync()
        {
            await webcamService.StopAsync();
        }

        #endregion Private Methods
    }
}