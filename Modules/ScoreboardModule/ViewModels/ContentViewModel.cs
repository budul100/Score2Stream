using Avalonia.Media;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.Commons.Prism;
using System.Collections.ObjectModel;
using System.Linq;

namespace Score2Stream.ScoreboardModule.ViewModels
{
    public class ContentViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly ScoreboardChangedEvent changedEvent;
        private readonly IContainerProvider containerProvider;
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

            changedEvent = eventAggregator.GetEvent<ScoreboardChangedEvent>();

            changedEvent.Subscribe(
                action: () => RaisePropertyChanged(nameof(TickersUpToDate)),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => UpdateValues(),
                keepSubscriberReferenceAlive: true);

            InitializeTickers();
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxLengthPeriod => 10;

        public static int MaxLengthScore => 10;

        public static int MaxLengthTeam => 20;

        public string ClockGame { get; private set; }

        public bool ClockNotFromClip
        {
            get { return scoreboardService.ClockNotFromClip; }
            set
            {
                scoreboardService.ClockNotFromClip = value;

                RaisePropertyChanged(nameof(ClockNotFromClip));
            }
        }

        public string ClockShot { get; private set; }

        public Color ColorGuest
        {
            get { return scoreboardService.ColorGuest; }
            set
            {
                if (!scoreboardService.ColorGuest.Equals(value))
                {
                    scoreboardService.ColorGuest = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(ColorGuest));
                    RaisePropertyChanged(nameof(ColorGuestUpToDate));
                }
            }
        }

        public bool ColorGuestUpToDate => scoreboardService.ColorGuestUpToDate;

        public Color ColorHome
        {
            get { return scoreboardService.ColorHome; }
            set
            {
                if (!scoreboardService.ColorHome.Equals(value))
                {
                    scoreboardService.ColorHome = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(ColorHome));
                    RaisePropertyChanged(nameof(ColorHomeUpToDate));
                }
            }
        }

        public bool ColorHomeUpToDate => scoreboardService.ColorHomeUpToDate;

        public bool IsGameOver
        {
            get { return scoreboardService.IsGameOver; }
            set
            {
                if (scoreboardService.IsGameOver != value)
                {
                    scoreboardService.IsGameOver = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(IsGameOver));
                    RaisePropertyChanged(nameof(IsGameOverUpToDate));
                }
            }
        }

        public bool IsGameOverUpToDate => scoreboardService.IsGameOverUpToDate;

        public string Period
        {
            get { return scoreboardService.Period; }
            set
            {
                if (scoreboardService.Period != value)
                {
                    scoreboardService.Period = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(Period));
                    RaisePropertyChanged(nameof(PeriodUpToDate));
                }
            }
        }

        public bool PeriodNotFromClip
        {
            get { return scoreboardService.PeriodNotFromClip; }
            set
            {
                if (scoreboardService.PeriodNotFromClip != value)
                {
                    scoreboardService.PeriodNotFromClip = value;

                    RaisePropertyChanged(nameof(PeriodNotFromClip));
                }
            }
        }

        public string Periods
        {
            get { return scoreboardService.Periods; }
            set
            {
                if (scoreboardService.Periods != value)
                {
                    scoreboardService.Periods = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(Periods));
                    RaisePropertyChanged(nameof(PeriodsUpToDate));
                }
            }
        }

        public bool PeriodsUpToDate => scoreboardService.PeriodsUpToDate;

        public bool PeriodUpToDate => scoreboardService.PeriodUpToDate;

        public string ScoreGuest
        {
            get { return scoreboardService.ScoreGuest; }
            set
            {
                if (scoreboardService.ScoreGuest != value)
                {
                    scoreboardService.ScoreGuest = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(ScoreGuest));
                    RaisePropertyChanged(nameof(ScoreGuestUpToDate));
                }
            }
        }

        public bool ScoreGuestUpToDate => scoreboardService.ScoreGuestUpToDate;

        public string ScoreHome
        {
            get { return scoreboardService.ScoreHome; }
            set
            {
                if (scoreboardService.ScoreHome != value)
                {
                    scoreboardService.ScoreHome = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(ScoreHome));
                    RaisePropertyChanged(nameof(ScoreHomeUpToDate));
                }
            }
        }

        public bool ScoreHomeUpToDate => scoreboardService.ScoreHomeUpToDate;

        public bool ScoreNotFromClip
        {
            get { return scoreboardService.ScoreNotFromClip; }
            set
            {
                if (scoreboardService.ScoreNotFromClip != value)
                {
                    scoreboardService.ScoreNotFromClip = value;

                    RaisePropertyChanged(nameof(ScoreNotFromClip));
                }
            }
        }

        public bool ShotNotFromClip
        {
            get { return scoreboardService.ShotNotFromClip; }
            set
            {
                scoreboardService.ShotNotFromClip = value;

                RaisePropertyChanged(nameof(ShotNotFromClip));
            }
        }

        public string TeamGuest
        {
            get { return scoreboardService.TeamGuest; }
            set
            {
                if (scoreboardService.TeamGuest != value)
                {
                    scoreboardService.TeamGuest = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(TeamGuest));
                    RaisePropertyChanged(nameof(TeamGuestUpToDate));
                }
            }
        }

        public bool TeamGuestUpToDate => scoreboardService.TeamGuestUpToDate;

        public string TeamHome
        {
            get { return scoreboardService.TeamHome; }
            set
            {
                if (scoreboardService.TeamHome != value)
                {
                    scoreboardService.TeamHome = value;
                    changedEvent.Publish();

                    RaisePropertyChanged(nameof(TeamHome));
                    RaisePropertyChanged(nameof(TeamHomeUpToDate));
                }
            }
        }

        public bool TeamHomeUpToDate => scoreboardService.TeamHomeUpToDate;

        public int TickersFrequency
        {
            get { return scoreboardService.TickersFrequency; }
            set
            {
                if (scoreboardService.TickersFrequency != value)
                {
                    scoreboardService.TickersFrequency = value;

                    RaisePropertyChanged(nameof(TickersFrequency));
                }
            }
        }

        public bool TickersUpToDate => scoreboardService
            .TickersUpToDate.All(t => t);

        #endregion Public Properties

        #region Private Properties

        private ObservableCollection<TickerViewModel> Tickers { get; } = new ObservableCollection<TickerViewModel>();

        #endregion Private Properties

        #region Private Methods

        private void InitializeTickers()
        {
            var settings = settingsService.Get();

            for (var index = 0; index < settings.Scoreboard.Tickers.Length; index++)
            {
                var current = containerProvider.Resolve<TickerViewModel>();

                current.Initialize(index);

                Tickers.Add(current);
            }
        }

        private void UpdateValues()
        {
            ClockGame = scoreboardService.ClockGame;
            ClockShot = scoreboardService.ClockShot;

            if (!PeriodNotFromClip)
            {
                Period = scoreboardService.Period;
            }

            if (!ScoreNotFromClip)
            {
                ScoreHome = scoreboardService.ScoreHome;
                ScoreGuest = scoreboardService.ScoreGuest;
            }

            RaisePropertyChanged(nameof(ClockGame));
            RaisePropertyChanged(nameof(ClockShot));

            RaisePropertyChanged(nameof(ColorGuest));
            RaisePropertyChanged(nameof(ColorGuestUpToDate));

            RaisePropertyChanged(nameof(ColorHome));
            RaisePropertyChanged(nameof(ColorHomeUpToDate));

            RaisePropertyChanged(nameof(IsGameOver));
            RaisePropertyChanged(nameof(IsGameOverUpToDate));

            RaisePropertyChanged(nameof(Period));
            RaisePropertyChanged(nameof(PeriodUpToDate));

            RaisePropertyChanged(nameof(Periods));
            RaisePropertyChanged(nameof(PeriodsUpToDate));

            RaisePropertyChanged(nameof(ScoreGuest));
            RaisePropertyChanged(nameof(ScoreGuestUpToDate));

            RaisePropertyChanged(nameof(ScoreHome));
            RaisePropertyChanged(nameof(ScoreHomeUpToDate));

            RaisePropertyChanged(nameof(TeamGuest));
            RaisePropertyChanged(nameof(TeamGuestUpToDate));

            RaisePropertyChanged(nameof(TeamHome));
            RaisePropertyChanged(nameof(TeamHomeUpToDate));

            RaisePropertyChanged(nameof(TickersUpToDate));
        }

        #endregion Private Methods
    }
}