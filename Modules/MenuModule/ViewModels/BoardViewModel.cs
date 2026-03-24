using Prism.Commands;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;

namespace Score2Stream.MenuModule.ViewModels
{
    public partial class MenuViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private IScoreboardService scoreboardService;

        #endregion Private Fields

        #region Public Properties

        public static int PortMax => Constants.PortMax;

        public static int PortMin => Constants.PortMin;

        public static string TabBoard => Constants.TabBoard;

        public bool AllowMultipleInstances
        {
            get { return settingsService.Contents.App.AllowMultipleInstances; }
            set
            {
                if (settingsService.Contents.App.AllowMultipleInstances != value)
                {
                    settingsService.Contents.App.AllowMultipleInstances = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(AllowMultipleInstances));
                }
            }
        }

        public int DelaySocket
        {
            get { return settingsService.Contents.Server.DelaySocket; }
            set
            {
                if (value >= DelayMin
                    && value <= DelayMax
                    && settingsService.Contents.Server.DelaySocket != value)
                {
                    settingsService.Contents.Server.DelaySocket = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(DelaySocket));
                }
            }
        }

        public bool IsUpToDate => scoreboardService.IsUpToDate;

        public int PortServer
        {
            get { return settingsService.Contents.Server.PortServer; }
            set
            {
                if (value >= PortMin
                    && value <= PortMax
                    && settingsService.Contents.Server.PortServer != value)
                {
                    settingsService.Contents.Server.PortServer = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(PortServer));
                }
            }
        }

        public int PortSocket
        {
            get { return settingsService.Contents.Server.PortSocket; }
            set
            {
                if (value >= PortMin
                    && value <= PortMax
                    && settingsService.Contents.Server.PortSocket != value)
                {
                    settingsService.Contents.Server.PortSocket = value;
                    settingsService.Save();

                    RaisePropertyChanged(nameof(PortSocket));
                }
            }
        }

        public DelegateCommand ScoreboardOpenCommand { get; private set; }

        public DelegateCommand ScoreboardUpdateCommand { get; private set; }

        public DelegateCommand ServerOpenCommand { get; private set; }

        public DelegateCommand ServerReloadCommand { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private partial void InitializeViewBoard(IScoreboardService scoreboardService, IWebService webService,
            IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;

            this.ServerOpenCommand = new DelegateCommand(
                executeMethod: webService.OpenRoot);
            this.ServerReloadCommand = new DelegateCommand(
                executeMethod: async () => await webService.ReloadAsync());
            this.ScoreboardOpenCommand = new DelegateCommand(
                executeMethod: webService.OpenServer,
                canExecuteMethod: () => webService.IsActive);
            this.ScoreboardUpdateCommand = new DelegateCommand(
                executeMethod: scoreboardService.Update,
                canExecuteMethod: () => !scoreboardService.IsUpToDate);

            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: OnServerStarted);

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(IsUpToDate)));
            eventAggregator.GetEvent<ScoreboardModifiedEvent>().Subscribe(
                action: ScoreboardUpdateCommand.RaiseCanExecuteChanged);
        }

        private void OnServerStarted()
        {
            ServerOpenCommand.RaiseCanExecuteChanged();
            ServerReloadCommand.RaiseCanExecuteChanged();

            ScoreboardOpenCommand.RaiseCanExecuteChanged();
            ScoreboardUpdateCommand.RaiseCanExecuteChanged();
        }

        #endregion Private Methods
    }
}