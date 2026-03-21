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

        private readonly ScoreboardModifiedEvent scoreboardModifiedEvent;
        private readonly IScoreboardService scoreboardService;

        private int number;

        #endregion Private Fields

        #region Public Constructors

        public TickerViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;

            scoreboardModifiedEvent = eventAggregator.GetEvent<ScoreboardModifiedEvent>();

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(UpToDate)),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxLengthTicker => 70;

        public bool IsActive
        {
            get => scoreboardService.Tickers[number].Item2;
            set
            {
                // Allow deactivating even when text is empty; only block activation without text
                if (value == scoreboardService.Tickers[number].Item2
                    || (value && string.IsNullOrWhiteSpace(Text))) return;

                scoreboardService.SetTicker(
                    number: number,
                    isActive: value);

                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(UpToDate));
            }
        }

        public string Text
        {
            get => scoreboardService.Tickers[number].Item1;
            set
            {
                if (value == scoreboardService.Tickers[number].Item1) return;

                scoreboardService.SetTicker(
                    number: number,
                    text: value);

                scoreboardModifiedEvent.Publish();

                RaisePropertyChanged(nameof(Text));
                RaisePropertyChanged(nameof(UpToDate));
            }
        }

        // TickersUpToDate null or empty => no tickers configured => treat as up to date
        public bool UpToDate => scoreboardService.TickersUpToDate is not { Length: > 0 }
            || scoreboardService.TickersUpToDate[number];

        #endregion Public Properties

        #region Public Methods

        public void Initialize(int number)
        {
            this.number = number;

            // Notify all properties explicitly; RaisePropertyChanged() without argument
            // uses CallerMemberName (= "Initialize") which no binding listens to.

            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(Text));
            RaisePropertyChanged(nameof(UpToDate));
        }

        #endregion Public Methods
    }
}