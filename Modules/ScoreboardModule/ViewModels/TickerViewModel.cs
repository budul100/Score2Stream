using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.ScoreboardModule.ViewModels
{
    public class TickerViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IScoreboardService scoreboardService;
        private int number;

        #endregion Private Fields

        #region Public Constructors

        public TickerViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(UpToDate)),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxLengthTicker => 70;

        public bool IsActive
        {
            get
            {
                return scoreboardService.Tickers[number].Item2;
            }
            set
            {
                if (value != scoreboardService.Tickers[number].Item2
                    && !string.IsNullOrWhiteSpace(Text))
                {
                    scoreboardService.SetTicker(
                        number: number,
                        isActive: value);

                    eventAggregator
                        .GetEvent<ScoreboardChangedEvent>()
                        .Publish();

                    RaisePropertyChanged(nameof(IsActive));
                    RaisePropertyChanged(nameof(UpToDate));
                }
            }
        }

        public string Text
        {
            get
            {
                return scoreboardService.Tickers[number].Item1;
            }
            set
            {
                if (value != scoreboardService.Tickers[number].Item1)
                {
                    scoreboardService.SetTicker(
                        number: number,
                        text: value);

                    eventAggregator
                        .GetEvent<ScoreboardChangedEvent>()
                        .Publish();

                    RaisePropertyChanged(nameof(Text));
                    RaisePropertyChanged(nameof(UpToDate));
                }
            }
        }

        public bool UpToDate => scoreboardService.TickersUpToDate[number];

        #endregion Public Properties

        #region Public Methods

        public void Initialize(int number)
        {
            this.number = number;

            RaisePropertyChanged();
        }

        #endregion Public Methods
    }
}