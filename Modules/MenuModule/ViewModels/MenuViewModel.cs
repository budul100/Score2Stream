using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.Commons.Prism;

namespace Score2Stream.MenuModule.ViewModels
{
    public partial class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IRegionManager regionManager;
        private readonly ISettingsService<Session> settingsService;
        private readonly TabSelectedEvent tabSelectedEvent;

        private int tabIndex;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(ISettingsService<Session> settingsService, IWebService webService,
            IScoreboardService scoreboardService, IInputService inputService, ITemplateService templateService,
            IRegionManager regionManager, IDialogService dialogService, IEventAggregator eventAggregator,
            ILogger<MenuViewModel> logger)
            : base(regionManager)
        {
            this.settingsService = settingsService;
            this.regionManager = regionManager;

            this.SelectTabCommand = new DelegateCommand<ViewType?>(
                executeMethod: t => TabIndex = (int?)t);

            tabSelectedEvent = eventAggregator.GetEvent<TabSelectedEvent>();

            InitializeViewInput(
                inputService: inputService,
                dialogService: dialogService,
                eventAggregator: eventAggregator,
                logger: logger);

            InitializeViewTemplate(
                templateService: templateService,
                eventAggregator: eventAggregator);

            InitializeViewBoard(
                scoreboardService: scoreboardService,
                webService: webService,
                eventAggregator: eventAggregator);
        }

        #endregion Public Constructors

        #region Public Properties

        public static int DelayMax => Constants.DelayMax;

        public static int DelayMin => Constants.DelayMin;

        public static int ThresholdMax => Constants.ThresholdMax;

        public DelegateCommand<ViewType?> SelectTabCommand { get; }

        public int? TabIndex
        {
            get { return tabIndex; }
            set
            {
                if (value.HasValue
                    && TabIndex != value)
                {
                    SetProperty(ref tabIndex, value.Value);

                    switch (tabIndex)
                    {
                        case (int)ViewType.Inputs:

                            IsSampleDetection = false;

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Inputs));

                            tabSelectedEvent.Publish(ViewType.Inputs);

                            OnAreasChanged();

                            break;

                        case (int)ViewType.Templates:

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Templates));

                            tabSelectedEvent.Publish(ViewType.Templates);

                            OnSamplesChanged();

                            break;

                        case (int)ViewType.Board:

                            IsSampleDetection = false;

                            regionManager.RequestNavigate(
                                regionName: nameof(RegionType.EditRegion),
                                source: nameof(ViewType.Board));

                            tabSelectedEvent.Publish(ViewType.Board);

                            break;
                    }
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private partial void InitializeViewBoard(IScoreboardService scoreboardService, IWebService webService,
            IEventAggregator eventAggregator);

        private partial void InitializeViewInput(IInputService inputService, IDialogService dialogService,
            IEventAggregator eventAggregator, ILogger<MenuViewModel> logger);

        private partial void InitializeViewTemplate(ITemplateService templateService, IEventAggregator eventAggregator);

        #endregion Private Methods
    }
}