using Core.Events.Scoreboard;
using Core.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System.Windows.Media;

namespace ScoreboardModule.ViewModels
{
    public class ContentViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IScoreboardService scoreboardService;

        private Color colorGuest;
        private Color colorHome;
        private bool isGameOver;
        private string period;
        private int? periods;
        private int scoreGuest;
        private int scoreHome;
        private string teamGuest;
        private string teamHome;

        #endregion Private Fields

        #region Public Constructors

        public ContentViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;

            ColorHome = new Color { A = 255 };
            ColorGuest = new Color { A = 255 };

            eventAggregator.GetEvent<UpdateScoreboardEvent>().Subscribe(
                action: UpdateScoreboard,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool ClockNotFromClip
        {
            get { return scoreboardService.ClockNotFromClip; }
            set
            {
                scoreboardService.ClockNotFromClip = value;
                RaisePropertyChanged(nameof(ClockNotFromClip));
            }
        }

        public Color ColorGuest
        {
            get { return colorGuest; }
            set
            {
                if (!colorGuest.Equals(value))
                {
                    SetProperty(ref colorGuest, value);

                    scoreboardService.Announce();
                }
            }
        }

        public Color ColorHome
        {
            get { return colorHome; }
            set
            {
                if (!colorHome.Equals(value))
                {
                    SetProperty(ref colorHome, value);

                    scoreboardService.Announce();
                }
            }
        }

        public bool IsGameOver
        {
            get { return isGameOver; }
            set
            {
                if (isGameOver != value)
                {
                    SetProperty(ref isGameOver, value);

                    scoreboardService.Announce();
                }
            }
        }

        public string Period
        {
            get { return period; }
            set
            {
                if (period != value)
                {
                    SetProperty(ref period, value);

                    scoreboardService.Announce();
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

                    scoreboardService.Announce();
                }
            }
        }

        public int? Periods
        {
            get { return periods; }
            set
            {
                if (periods != value)
                {
                    SetProperty(ref periods, value);

                    scoreboardService.Announce();
                }
            }
        }

        public int ScoreGuest
        {
            get { return scoreGuest; }
            set
            {
                if (scoreGuest != value)
                {
                    SetProperty(ref scoreGuest, value);
                    scoreboardService.Announce();
                }
            }
        }

        public int ScoreHome
        {
            get { return scoreHome; }
            set
            {
                if (scoreHome != value)
                {
                    SetProperty(ref scoreHome, value);

                    scoreboardService.Announce();
                }
            }
        }

        public bool ScoreNotFromClip
        {
            get { return scoreboardService.ScoreNotFromClip; }
            set
            {
                if (scoreboardService.ScoreNotFromClip != value)
                {
                    scoreboardService.ScoreNotFromClip = value;
                    RaisePropertyChanged(nameof(ScoreNotFromClip));

                    scoreboardService.Announce();
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
            get { return teamGuest; }
            set
            {
                if (teamGuest != value)
                {
                    SetProperty(ref teamGuest, value);

                    scoreboardService.Announce();
                }
            }
        }

        public string TeamHome
        {
            get { return teamHome; }
            set
            {
                if (teamHome != value)
                {
                    SetProperty(ref teamHome, value);

                    scoreboardService.Announce();
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void UpdateScoreboard()
        {
            var colorHomeHex = $"#{ColorHome.R:X2}{ColorHome.G:X2}{ColorHome.B:X2}";
            var colorGuestHex = $"#{ColorGuest.R:X2}{ColorGuest.G:X2}{ColorGuest.B:X2}";

            scoreboardService.Update(
                period: Period,
                periods: Periods,
                isGameOver: IsGameOver,
                teamHome: TeamHome,
                teamGuest: TeamGuest,
                scoreHome: ScoreHome,
                scoreGuest: ScoreGuest,
                colorHome: colorHomeHex,
                colorGuest: colorGuestHex,
                tickers: default);
        }

        #endregion Private Methods
    }
}