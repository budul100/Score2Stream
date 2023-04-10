using Core.Events.Scoreboard;
using Core.Interfaces;
using Prism.Events;
using Prism.Mvvm;

namespace ScoreboardModule.ViewModels
{
    public class ContentViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IScoreboardService scoreboardService;

        private bool isGameOver;
        private string period;
        private string periods;

        #endregion Private Fields

        #region Public Constructors

        public ContentViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;

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
                scoreboardService.PeriodNotFromClip = value;
                RaisePropertyChanged(nameof(PeriodNotFromClip));
            }
        }

        public string Periods
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

        public bool ShotNotFromClip
        {
            get { return scoreboardService.ShotNotFromClip; }
            set
            {
                scoreboardService.ShotNotFromClip = value;
                RaisePropertyChanged(nameof(ShotNotFromClip));
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void UpdateScoreboard()
        {
            scoreboardService.Update(
                period: Period,
                periods: Periods,
                isGameOver: IsGameOver,
                teamHome: default,
                teamGuest: default,
                scoreHome: default,
                scoreGuest: default,
                tickers: default);
        }

        #endregion Private Methods
    }
}