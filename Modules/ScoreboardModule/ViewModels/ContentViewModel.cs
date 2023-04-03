using Core.Events;
using Core.Interfaces;
using Core.Prism;
using Prism.Events;
using System;

namespace ScoreboardModule.ViewModels
{
    public class ContentViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IScoreboardService scoreboardService;

        #endregion Private Fields

        #region Public Constructors

        public ContentViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsGameOver
        {
            get { return scoreboardService.IsGameOver; }
            set
            {
                scoreboardService.IsGameOver = value; ;

                RaisePropertyChanged(nameof(IsGameOver));
                eventAggregator.GetEvent<UpdateScoreboardEvent>().Publish();
            }
        }

        public string Period
        {
            get { return scoreboardService.Period.ToString(); }
            set
            {
                if (Int32.TryParse(value, out int result))
                {
                    scoreboardService.Period = result; ;
                }
                else
                {
                    scoreboardService.Period = default;
                }

                RaisePropertyChanged(nameof(Period));
                eventAggregator.GetEvent<UpdateScoreboardEvent>().Publish();
            }
        }

        public string ScoreGuest
        {
            get { return scoreboardService.ScoreGuest.ToString(); }
            set
            {
                if (Int32.TryParse(value, out int result))
                {
                    scoreboardService.ScoreGuest = result; ;
                }
                else
                {
                    scoreboardService.ScoreGuest = default;
                }

                RaisePropertyChanged(nameof(ScoreGuest));
                eventAggregator.GetEvent<UpdateScoreboardEvent>().Publish();
            }
        }

        public string ScoreHome
        {
            get { return scoreboardService.ScoreHome.ToString(); }
            set
            {
                if (Int32.TryParse(value, out int result))
                {
                    scoreboardService.ScoreHome = result; ;
                }
                else
                {
                    scoreboardService.ScoreHome = default;
                }

                RaisePropertyChanged(nameof(ScoreHome));
                eventAggregator.GetEvent<UpdateScoreboardEvent>().Publish();
            }
        }

        public string TeamGuest
        {
            get { return scoreboardService.TeamGuest; }
            set
            {
                scoreboardService.TeamGuest = value;

                RaisePropertyChanged(nameof(TeamGuest));
                eventAggregator.GetEvent<UpdateScoreboardEvent>().Publish();
            }
        }

        public string TeamHome
        {
            get { return scoreboardService.TeamHome; }
            set
            {
                scoreboardService.TeamHome = value;

                RaisePropertyChanged(nameof(TeamHome));
                eventAggregator.GetEvent<UpdateScoreboardEvent>().Publish();
            }
        }

        public string Ticker
        {
            get { return scoreboardService.Ticker; }
            set
            {
                scoreboardService.Ticker = value;

                RaisePropertyChanged(nameof(Ticker));
                eventAggregator.GetEvent<UpdateScoreboardEvent>().Publish();
            }
        }

        #endregion Public Properties
    }
}