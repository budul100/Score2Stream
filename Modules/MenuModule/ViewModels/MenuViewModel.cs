﻿using Core.Constants;
using Core.Events;
using Core.Interfaces;
using Core.Mvvm;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Linq;

namespace MenuModule.ViewModels
{
    public class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private const int ViewIndexClip = 1;
        private const int ViewIndexContent = 3;
        private const int ViewIndexTemplate = 2;

        private readonly IEventAggregator eventAggregator;
        private readonly IRegionManager regionManager;
        private readonly ITemplateService templateService;
        private readonly IWebcamService webcamService;
        private int selectedTabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IWebcamService webcamService, IClipService clipService, ITemplateService templateService,
            IGraphicsService graphicsService, IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.webcamService = webcamService;
            this.templateService = templateService;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ClipsChangedEvent>().Subscribe(
                action: OnClipsChanged);
            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: _ => OnClipsChanged());

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: OnTemplatesChanged);
            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected());

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: _ => OnSampleSelected());

            eventAggregator.GetEvent<GraphicsUpdatedEvent>().Subscribe(
                action: OnGraphicsUpdated);
            eventAggregator.GetEvent<WebcamUpdatedEvent>().Subscribe(
                action: OnWebcamUpdated);

            this.WebcamPlayCommand = new DelegateCommand(
                executeMethod: async () => await webcamService.StartAsync(),
                canExecuteMethod: () => !webcamService.IsActive);
            this.WebcamPauseCommand = new DelegateCommand(
                executeMethod: async () => await webcamService.StopAsync(),
                canExecuteMethod: () => webcamService.IsActive);

            this.GraphicsStartCommand = new DelegateCommand(
                executeMethod: async () => await graphicsService.StartAsync(GraphicsUrls.PortHttpWebServer, GraphicsUrls.PortHttpWebSocket),
                canExecuteMethod: () => !graphicsService.IsActive);
            this.GraphicsEndCommand = new DelegateCommand(
                executeMethod: async () => await graphicsService.StopAsync(),
                canExecuteMethod: () => graphicsService.IsActive);
            this.GraphicsOpenCommand = new DelegateCommand(
                executeMethod: () => graphicsService.Open(),
                canExecuteMethod: () => graphicsService.IsActive);

            this.ClipAddCommand = new DelegateCommand(
                executeMethod: () => clipService.Add(),
                canExecuteMethod: () => webcamService.IsActive);
            this.ClipRemoveCommand = new DelegateCommand(
                executeMethod: () => clipService.Remove(),
                canExecuteMethod: () => clipService.Clip != default);

            var addTemplateEvent = eventAggregator.GetEvent<SelectTemplateEvent>();
            this.ClipAsTemplateCommand = new DelegateCommand(
                executeMethod: () => addTemplateEvent.Publish(clipService.Clip),
                canExecuteMethod: () => clipService.Clip?.Content != default);

            this.TemplateRemoveCommand = new DelegateCommand(
                executeMethod: () => templateService.RemoveTemplate(),
                canExecuteMethod: () => templateService.Template != default);

            this.SampleAddCommand = new DelegateCommand(
                executeMethod: () => templateService.AddSample(),
                canExecuteMethod: () => templateService.Template != default);
            this.SampleRemoveCommand = new DelegateCommand(
                executeMethod: () => templateService.RemoveSample(),
                canExecuteMethod: () => templateService.Sample != default);
        }

        #endregion Public Constructors

        #region Public Properties

        public int CameraDeviceId
        {
            get { return webcamService.CameraDeviceId; }
            set
            {
                webcamService.CameraDeviceId = value;
                RaisePropertyChanged(nameof(CameraDeviceId));
            }
        }

        public DelegateCommand ClipAddCommand { get; }

        public DelegateCommand ClipAsTemplateCommand { get; }

        public DelegateCommand ClipRemoveCommand { get; }

        public DelegateCommand GraphicsEndCommand { get; }

        public DelegateCommand GraphicsOpenCommand { get; }

        public DelegateCommand GraphicsStartCommand { get; }

        public bool HasTemplates => templateService.Templates.Any();

        public DelegateCommand SampleAddCommand { get; }

        public DelegateCommand SampleRemoveCommand { get; }

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                if (SelectedTabIndex != value)
                {
                    SetProperty(ref selectedTabIndex, value);

                    SetEditRegion();
                }
            }
        }

        public DelegateCommand TemplateRemoveCommand { get; }

        public ObservableCollection<TemplateViewModel> Templates { get; } =
            new ObservableCollection<TemplateViewModel>();

        public int ThresholdCompare
        {
            get { return webcamService.ThresholdCompare; }
            set
            {
                webcamService.ThresholdCompare = value;
                RaisePropertyChanged(nameof(ThresholdCompare));
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

        private void OnClipsChanged()
        {
            ClipAddCommand.RaiseCanExecuteChanged();
            ClipRemoveCommand.RaiseCanExecuteChanged();

            ClipAsTemplateCommand.RaiseCanExecuteChanged();
        }

        private void OnGraphicsUpdated()
        {
            GraphicsStartCommand.RaiseCanExecuteChanged();
            GraphicsEndCommand.RaiseCanExecuteChanged();
            GraphicsOpenCommand.RaiseCanExecuteChanged();
        }

        private void OnSampleSelected()
        {
            SampleRemoveCommand.RaiseCanExecuteChanged();
        }

        private void OnTemplatesChanged()
        {
            Templates.Clear();

            var relevants = templateService.Templates
                .OrderBy(t => t.Clip.Name).ToArray();

            foreach (var relevant in relevants)
            {
                var current = new TemplateViewModel(
                    clip: relevant.Clip,
                    eventAggregator: eventAggregator);

                Templates.Add(current);
            }

            RaisePropertyChanged(nameof(HasTemplates));

            OnTemplateSelected();
        }

        private void OnTemplateSelected()
        {
            SelectedTabIndex = templateService.Template != default
                ? ViewIndexTemplate
                : ViewIndexClip;

            TemplateRemoveCommand.RaiseCanExecuteChanged();

            SampleAddCommand.RaiseCanExecuteChanged();
        }

        private void OnWebcamUpdated()
        {
            WebcamPlayCommand.RaiseCanExecuteChanged();
            WebcamPauseCommand.RaiseCanExecuteChanged();

            ClipAddCommand.RaiseCanExecuteChanged();
            ClipAsTemplateCommand.RaiseCanExecuteChanged();
        }

        private void SetEditRegion()
        {
            switch (SelectedTabIndex)
            {
                case ViewIndexClip:

                    regionManager.RequestNavigate(
                        regionName: RegionNames.EditRegion,
                        source: ViewNames.ClipView);

                    break;

                case ViewIndexTemplate:

                    regionManager.RequestNavigate(
                        regionName: RegionNames.EditRegion,
                        source: ViewNames.TemplateView);
                    break;

                case ViewIndexContent:

                    regionManager.RequestNavigate(
                        regionName: RegionNames.EditRegion,
                        source: ViewNames.ContentView);
                    break;
            }
        }

        #endregion Private Methods
    }
}