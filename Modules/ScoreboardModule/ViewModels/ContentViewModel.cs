using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.Commons.Prism;

namespace Score2Stream.ScoreboardModule.ViewModels
{
    public class ContentViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly ScoreboardModifiedEvent scoreboardModifiedEvent;
        private readonly IScoreboardService scoreboardService;
        private readonly ISettingsService<Session> settingsService;

        #endregion Private Fields

        #region Public Constructors

        public ContentViewModel(ISettingsService<Session> settingsService, IScoreboardService scoreboardService,
            IContainerProvider containerProvider, IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager: regionManager)
        {
            this.settingsService = settingsService;
            this.scoreboardService = scoreboardService;
            this.containerProvider = containerProvider;

            scoreboardModifiedEvent = eventAggregator.GetEvent<ScoreboardModifiedEvent>();

            scoreboardModifiedEvent.Subscribe(
                action: () => RaisePropertyChanged(nameof(TickersUpToDate)),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => UpdateValues(),
                keepSubscriberReferenceAlive: true);

            InitializeTickers();
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxLengthFouls => 10;

        public static int MaxLengthPeriod => 10;

        public static int MaxLengthScore => 10;

        public static int MaxLengthTeam => 20;

        public string ClockGame { get; private set; }

        public bool ClockNotDetected
        {
            get => scoreboardService.ClockNotDetected;
            set
            {
                if (scoreboardService.ClockNotDetected == value) return;

                scoreboardService.ClockNotDetected = value;
                RaisePropertyChanged(nameof(ClockNotDetected));
            }
        }

        public string ClockShot { get; private set; }

        public Color ColorGuest
        {
            get => scoreboardService.ColorGuest;
            set
            {
                if (scoreboardService.ColorGuest.Equals(value)) return;

                scoreboardService.ColorGuest = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(ColorGuest));
                RaisePropertyChanged(nameof(ColorGuestUpToDate));
            }
        }

        public bool ColorGuestUpToDate => scoreboardService.ColorGuestUpToDate;

        public Color ColorHome
        {
            get => scoreboardService.ColorHome;
            set
            {
                if (scoreboardService.ColorHome.Equals(value)) return;

                scoreboardService.ColorHome = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(ColorHome));
                RaisePropertyChanged(nameof(ColorHomeUpToDate));
            }
        }

        public bool ColorHomeUpToDate => scoreboardService.ColorHomeUpToDate;

        public string FoulsGuest
        {
            get => scoreboardService.FoulsGuest;
            set
            {
                if (scoreboardService.FoulsGuest == value) return;

                scoreboardService.FoulsGuest = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(FoulsGuest));
                RaisePropertyChanged(nameof(FoulsGuestUpToDate));
            }
        }

        public bool FoulsGuestUpToDate => scoreboardService.FoulsGuestUpToDate;

        public string FoulsHome
        {
            get => scoreboardService.FoulsHome;
            set
            {
                if (scoreboardService.FoulsHome == value) return;

                scoreboardService.FoulsHome = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(FoulsHome));
                RaisePropertyChanged(nameof(FoulsHomeUpToDate));
            }
        }

        public bool FoulsHomeUpToDate => scoreboardService.FoulsHomeUpToDate;

        public bool FoulsNotDetected
        {
            get => scoreboardService.FoulsNotDetected;
            set
            {
                if (scoreboardService.FoulsNotDetected == value) return;

                scoreboardService.FoulsNotDetected = value;
                RaisePropertyChanged(nameof(FoulsNotDetected));
            }
        }

        public bool IsGameOver
        {
            get => scoreboardService.IsGameOver;
            set
            {
                if (scoreboardService.IsGameOver == value) return;

                scoreboardService.IsGameOver = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(IsGameOver));
                RaisePropertyChanged(nameof(IsGameOverUpToDate));
            }
        }

        public bool IsGameOverUpToDate => scoreboardService.IsGameOverUpToDate;

        public string Period
        {
            get => scoreboardService.Period;
            set
            {
                if (scoreboardService.Period == value) return;

                scoreboardService.Period = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(Period));
                RaisePropertyChanged(nameof(PeriodUpToDate));
            }
        }

        public bool PeriodNotDetected
        {
            get => scoreboardService.PeriodNotDetected;
            set
            {
                if (scoreboardService.PeriodNotDetected == value) return;

                scoreboardService.PeriodNotDetected = value;
                RaisePropertyChanged(nameof(PeriodNotDetected));
            }
        }

        public string Periods
        {
            get => scoreboardService.Periods;
            set
            {
                if (scoreboardService.Periods == value) return;

                scoreboardService.Periods = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(Periods));
                RaisePropertyChanged(nameof(PeriodsUpToDate));
            }
        }

        public bool PeriodsUpToDate => scoreboardService.PeriodsUpToDate;

        public bool PeriodUpToDate => scoreboardService.PeriodUpToDate;

        public string ScoreGuest
        {
            get => scoreboardService.ScoreGuest;
            set
            {
                if (scoreboardService.ScoreGuest == value) return;

                scoreboardService.ScoreGuest = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(ScoreGuest));
                RaisePropertyChanged(nameof(ScoreGuestUpToDate));
            }
        }

        public bool ScoreGuestUpToDate => scoreboardService.ScoreGuestUpToDate;

        public string ScoreHome
        {
            get => scoreboardService.ScoreHome;
            set
            {
                if (scoreboardService.ScoreHome == value) return;

                scoreboardService.ScoreHome = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(ScoreHome));
                RaisePropertyChanged(nameof(ScoreHomeUpToDate));
            }
        }

        public bool ScoreHomeUpToDate => scoreboardService.ScoreHomeUpToDate;

        public bool ScoreNotDetected
        {
            get => scoreboardService.ScoreNotDetected;
            set
            {
                if (scoreboardService.ScoreNotDetected == value) return;

                scoreboardService.ScoreNotDetected = value;
                RaisePropertyChanged(nameof(ScoreNotDetected));
            }
        }

        public bool ShotNotDetected
        {
            get => scoreboardService.ShotNotDetected;
            set
            {
                if (scoreboardService.ShotNotDetected == value) return;

                scoreboardService.ShotNotDetected = value;
                RaisePropertyChanged(nameof(ShotNotDetected));
            }
        }

        public bool ShowTenthOfSecs
        {
            get => scoreboardService.ShowTenthOfSecs;
            set
            {
                if (scoreboardService.ShowTenthOfSecs == value) return;

                scoreboardService.ShowTenthOfSecs = value;
                RaisePropertyChanged(nameof(ShowTenthOfSecs));
            }
        }

        public string TeamGuest
        {
            get => scoreboardService.TeamGuest;
            set
            {
                if (scoreboardService.TeamGuest == value) return;

                scoreboardService.TeamGuest = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(TeamGuest));
                RaisePropertyChanged(nameof(TeamGuestUpToDate));
            }
        }

        public bool TeamGuestUpToDate => scoreboardService.TeamGuestUpToDate;

        public string TeamHome
        {
            get => scoreboardService.TeamHome;
            set
            {
                if (scoreboardService.TeamHome == value) return;

                scoreboardService.TeamHome = value;
                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(TeamHome));
                RaisePropertyChanged(nameof(TeamHomeUpToDate));
            }
        }

        public bool TeamHomeUpToDate => scoreboardService.TeamHomeUpToDate;

        public ObservableCollection<TickerViewModel> Tickers { get; } = [];

        public int TickersFrequency
        {
            get => scoreboardService.TickersFrequency;
            set
            {
                if (scoreboardService.TickersFrequency == value) return;

                scoreboardService.TickersFrequency = value;
                RaisePropertyChanged(nameof(TickersFrequency));
            }
        }

        public bool TickersUpToDate => scoreboardService.TickersUpToDate.All(t => t);

        #endregion Public Properties

        #region Private Methods

        private void InitializeTickers()
        {
            var length = settingsService.Contents.Scoreboard.Tickers.Length;

            for (var index = 0; index < length; index++)
            {
                var current = containerProvider.Resolve<TickerViewModel>();

                current.Initialize(index);

                Tickers.Add(current);
            }
        }

        private void UpdateValues()
        {
            RaisePropertyChanged(nameof(TeamGuest));
            RaisePropertyChanged(nameof(TeamGuestUpToDate));
            RaisePropertyChanged(nameof(TeamHome));
            RaisePropertyChanged(nameof(TeamHomeUpToDate));

            RaisePropertyChanged(nameof(ColorGuest));
            RaisePropertyChanged(nameof(ColorGuestUpToDate));
            RaisePropertyChanged(nameof(ColorHome));
            RaisePropertyChanged(nameof(ColorHomeUpToDate));

            ClockGame = scoreboardService.ClockGame;
            ClockShot = scoreboardService.ClockShot;

            RaisePropertyChanged(nameof(ClockGame));
            RaisePropertyChanged(nameof(ClockShot));

            Period = scoreboardService.Period;

            RaisePropertyChanged(nameof(Period));
            RaisePropertyChanged(nameof(PeriodUpToDate));
            RaisePropertyChanged(nameof(PeriodsUpToDate));

            RaisePropertyChanged(nameof(IsGameOver));
            RaisePropertyChanged(nameof(IsGameOverUpToDate));

            ScoreHome = scoreboardService.ScoreHome;
            ScoreGuest = scoreboardService.ScoreGuest;

            RaisePropertyChanged(nameof(ScoreHome));
            RaisePropertyChanged(nameof(ScoreHomeUpToDate));
            RaisePropertyChanged(nameof(ScoreGuest));
            RaisePropertyChanged(nameof(ScoreGuestUpToDate));

            FoulsHome = scoreboardService.FoulsHome;
            FoulsGuest = scoreboardService.FoulsGuest;

            RaisePropertyChanged(nameof(FoulsHome));
            RaisePropertyChanged(nameof(FoulsHomeUpToDate));
            RaisePropertyChanged(nameof(FoulsGuest));
            RaisePropertyChanged(nameof(FoulsGuestUpToDate));

            RaisePropertyChanged(nameof(TickersUpToDate));
        }

        #endregion Private Methods
    }
}