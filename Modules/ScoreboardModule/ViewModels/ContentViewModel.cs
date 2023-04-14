using Avalonia.Media;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Core.Events.Scoreboard;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Prism;
using System.Collections.Generic;

namespace Score2Stream.ScoreboardModule.ViewModels
{
    public class ContentViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly ScoreboardChangedEvent changedEvent;
        private readonly IScoreboardService scoreboardService;

        private string ticker1;
        private bool ticker1Active;
        private string ticker2;
        private bool ticker2Active;
        private string ticker3;
        private bool ticker3Active;
        private string ticker4;
        private bool ticker4Active;
        private string ticker5;
        private bool ticker5Active;

        #endregion Private Fields

        #region Public Constructors

        public ContentViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator,
            IRegionManager regionManager)
            : base(regionManager: regionManager)
        {
            this.scoreboardService = scoreboardService;

            changedEvent = eventAggregator.GetEvent<ScoreboardChangedEvent>();

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => UpdateValues(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

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

        public string Ticker1
        {
            get { return ticker1; }
            set
            {
                SetProperty(ref ticker1, value);
                UpdateTickers();
            }
        }

        public bool Ticker1Active
        {
            get { return ticker1Active; }
            set
            {
                SetProperty(ref ticker1Active, value);
                UpdateTickers();
            }
        }

        public string Ticker2
        {
            get { return ticker2; }
            set
            {
                SetProperty(ref ticker2, value);
                UpdateTickers();
            }
        }

        public bool Ticker2Active
        {
            get { return ticker2Active; }
            set
            {
                SetProperty(ref ticker2Active, value);
                UpdateTickers();
            }
        }

        public string Ticker3
        {
            get { return ticker3; }
            set
            {
                SetProperty(ref ticker3, value);
                UpdateTickers();
            }
        }

        public bool Ticker3Active
        {
            get { return ticker3Active; }
            set
            {
                SetProperty(ref ticker3Active, value);
                UpdateTickers();
            }
        }

        public string Ticker4
        {
            get { return ticker4; }
            set
            {
                SetProperty(ref ticker4, value);
                UpdateTickers();
            }
        }

        public bool Ticker4Active
        {
            get { return ticker4Active; }
            set
            {
                SetProperty(ref ticker4Active, value);
                UpdateTickers();
            }
        }

        public string Ticker5
        {
            get { return ticker5; }
            set
            {
                SetProperty(ref ticker5, value);
                UpdateTickers();
            }
        }

        public bool Ticker5Active
        {
            get { return ticker5Active; }
            set
            {
                SetProperty(ref ticker5Active, value);
                UpdateTickers();
            }
        }

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

        public bool TickersUpToDate => scoreboardService.TickersUpToDate;

        #endregion Public Properties

        #region Private Methods

        private void UpdateTickers()
        {
            var tickers = new HashSet<string>();

            if (Ticker1Active
                && !string.IsNullOrWhiteSpace(ticker1))
            {
                tickers.Add(ticker1.Trim());
            }

            if (Ticker2Active
                && !string.IsNullOrWhiteSpace(ticker2))
            {
                tickers.Add(ticker2.Trim());
            }

            if (Ticker3Active
                && !string.IsNullOrWhiteSpace(ticker3))
            {
                tickers.Add(ticker3.Trim());
            }

            if (Ticker4Active
                && !string.IsNullOrWhiteSpace(ticker4))
            {
                tickers.Add(ticker4.Trim());
            }

            if (Ticker5Active
                && !string.IsNullOrWhiteSpace(ticker5))
            {
                tickers.Add(ticker5.Trim());
            }

            scoreboardService.Tickers = tickers;
            changedEvent.Publish();

            RaisePropertyChanged(nameof(TickersUpToDate));
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