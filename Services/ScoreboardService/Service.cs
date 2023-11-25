using Avalonia.Media;
using Prism.Events;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Events.Video;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Scoreboard;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.ScoreboardService.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Score2Stream.ScoreboardService
{
    public class Service
        : IScoreboardService
    {
        #region Private Fields

        private const char GameClockSplitterDefault = ':';

        private readonly IDictionary<ClipType, Clip> clips = new Dictionary<ClipType, Clip>();
        private readonly IEnumerable<ClipType> clipTypes;
        private readonly IEventAggregator eventAggregator;
        private readonly JsonSerializerOptions serializeOptions;
        private readonly Session settings;
        private readonly ISettingsService<Session> settingsService;

        private string clockGame;
        private string clockShot;
        private Color colorGuest;
        private Color colorHome;
        private bool isGameOver;
        private string period;
        private string periods;
        private string scoreGuest;
        private string scoreHome;
        private string teamGuest;
        private string teamHome;
        private string ticker;
        private DateTime? tickerLastUpdate;
        private IEnumerable<(string, bool)> tickers;
        private int tickersInd;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.eventAggregator = eventAggregator;

            settings = settingsService.Get();

            serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            // Send first message to keep the web socket running
            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            clipTypes = GetClipTypes().ToArray();

            Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<ClipType> ClipTypes => clipTypes
            .Where(t => t == default || (clips.ContainsKey(t) && clips[t] == default)).ToArray();

        public string ClockGame { get; private set; }

        public bool ClockGameIsUpToDate => ClockGame == clockGame;

        public bool ClockNotFromClip { get; set; }

        public string ClockShot { get; private set; }

        public bool ClockShotIsUpToDate => ClockShot == clockShot;

        public Color ColorGuest
        {
            get
            {
                var result = Color.Parse(settings.Scoreboard.ColorGuest);

                return result;
            }
            set
            {
                var color = value.ToString();

                if (color != settings.Scoreboard.ColorGuest)
                {
                    settings.Scoreboard.ColorGuest = color;
                    settingsService.Save();
                }
            }
        }

        public bool ColorGuestUpToDate => ColorGuest == colorGuest;

        public Color ColorHome
        {
            get
            {
                var result = Color.Parse(settings.Scoreboard.ColorHome);

                return result;
            }
            set
            {
                var color = value.ToString();

                if (color != settings.Scoreboard.ColorHome)
                {
                    settings.Scoreboard.ColorHome = color;
                    settingsService.Save();
                }
            }
        }

        public bool ColorHomeUpToDate => ColorHome == colorHome;

        public bool IsGameOver { get; set; }

        public bool IsGameOverUpToDate => IsGameOver == isGameOver;

        public string Message { get; private set; }

        public string Period { get; set; }

        public bool PeriodNotFromClip { get; set; }

        public string Periods
        {
            get { return settings.Scoreboard.Periods; }
            set
            {
                if (value != settings.Scoreboard.Periods)
                {
                    settings.Scoreboard.Periods = value;
                    settingsService.Save();
                }
            }
        }

        public bool PeriodsUpToDate => Periods == periods;

        public bool PeriodUpToDate => Period == period;

        public string ScoreGuest { get; set; }

        public bool ScoreGuestUpToDate => ScoreGuest == scoreGuest;

        public string ScoreHome { get; set; }

        public bool ScoreHomeUpToDate => ScoreHome == scoreHome;

        public bool ScoreNotFromClip { get; set; }

        public bool ShotNotFromClip { get; set; }

        public string TeamGuest
        {
            get { return settings.Scoreboard.TeamGuest; }
            set
            {
                if (value != settings.Scoreboard.TeamGuest)
                {
                    settings.Scoreboard.TeamGuest = value;
                    settingsService.Save();
                }
            }
        }

        public bool TeamGuestUpToDate => TeamGuest == teamGuest;

        public string TeamHome
        {
            get { return settings.Scoreboard.TeamHome; }
            set
            {
                if (value != settings.Scoreboard.TeamHome)
                {
                    settings.Scoreboard.TeamHome = value;
                    settingsService.Save();
                }
            }
        }

        public bool TeamHomeUpToDate => TeamHome == teamHome;

        public (string, bool)[] Tickers => settings.Scoreboard.Tickers;

        public int TickersFrequency
        {
            get { return settings.Scoreboard.TickersFrequency; }
            set
            {
                if (settings.Scoreboard.TickersFrequency != value)
                {
                    settings.Scoreboard.TickersFrequency = value;

                    settingsService.Save();
                }
            }
        }

        public bool[] TickersUpToDate { get; private set; }

        public bool UpToDate => ColorGuestUpToDate
            && ColorHomeUpToDate
            && IsGameOverUpToDate
            && PeriodsUpToDate
            && PeriodUpToDate
            && ScoreGuestUpToDate
            && ScoreHomeUpToDate
            && TeamGuestUpToDate
            && TeamHomeUpToDate
            && TickersUpToDate.All(t => t);

        #endregion Public Properties

        #region Public Methods

        public void RemoveClip(Clip clip)
        {
            if (clip != default)
            {
                clips[clip.Type] = default;
                clip.Type = default;
            }
        }

        public void SetClip(Clip clip, ClipType clipType)
        {
            if (clip != default)
            {
                if (clip.Type != ClipType.None
                    && clips[clip.Type] != default
                    && clips[clip.Type].Type != ClipType.None)
                {
                    var current = clips[clip.Type];

                    clips[clip.Type] = default;
                    current.Type = ClipType.None;
                }

                if (clipType != ClipType.None)
                {
                    clips[clipType] = clip;
                }

                clip.Type = clipType;

                eventAggregator.GetEvent<ClipUpdatedEvent>().Publish(clip);
            }
        }

        public void SetTicker(int number, string text)
        {
            if (settings.Scoreboard.Tickers.Length > number)
            {
                settings.Scoreboard.Tickers[number].Item1 = text;
                settingsService.Save();

                TickersUpToDate = GetTickersUpToDate().ToArray();
            }
        }

        public void SetTicker(int number, bool isActive)
        {
            if (settings.Scoreboard.Tickers.Length > number)
            {
                settings.Scoreboard.Tickers[number].Item2 = isActive;
                settingsService.Save();

                TickersUpToDate = GetTickersUpToDate().ToArray();
            }
        }

        public void Update()
        {
            this.periods = Periods;
            this.isGameOver = IsGameOver;
            this.teamHome = TeamHome;
            this.teamGuest = TeamGuest;
            this.colorHome = ColorHome;
            this.colorGuest = ColorGuest;

            if (PeriodNotFromClip
                || clips[ClipType.Period] == default)
            {
                period = Period;
            }

            if (ScoreNotFromClip
                || clips[ClipType.ScoreHome1] == default
                || clips[ClipType.ScoreHome2] == default
                || clips[ClipType.ScoreHome3] == default
                || clips[ClipType.ScoreGuest1] == default
                || clips[ClipType.ScoreGuest2] == default
                || clips[ClipType.ScoreGuest3] == default)
            {
                scoreHome = ScoreHome;
                scoreGuest = ScoreGuest;
            }

            tickers = Tickers.ToArray();
            TickersUpToDate = GetTickersUpToDate().ToArray();

            UpdateTicker();
            UpdateBoard();
        }

        #endregion Public Methods

        #region Private Methods

        private Board GetBoard()
        {
            var game = new Game
            {
                Clock = clockGame,
                Possesion = default,
                Period = period,
                Periods = periods,
                Shot = clockShot,
            };

            var home = new Home
            {
                Color = colorHome.GetColorHex(),
                Fouls = default,
                ImagePath = default,
                Name = teamHome,
                Score = scoreHome,
            };

            var guest = new Guest
            {
                Color = colorGuest.GetColorHex(),
                Fouls = default,
                ImagePath = default,
                Name = teamGuest,
                Score = scoreGuest,
            };

            var result = new Board
            {
                Game = game,
                Guest = guest,
                Home = home,
                GameID = default,
                GameOver = isGameOver,
                Ticker = ticker,
            };

            return result;
        }

        private IEnumerable<ClipType> GetClipTypes()
        {
            var types = Commons.Extensions.EnumExtensions
                .GetValues<ClipType>().ToArray();

            foreach (var type in types)
            {
                if (type != ClipType.None)
                {
                    clips.Add(
                        key: type,
                        value: default);
                }

                yield return type;
            }
        }

        private string GetClockGame()
        {
            var result = new StringBuilder();

            if (clips[ClipType.ClockGameMin1] != default)
            {
                result.Append(clips[ClipType.ClockGameMin1].Value);
            }
            if (clips[ClipType.ClockGameMin2] != default)
            {
                result.Append(clips[ClipType.ClockGameMin2].Value);
            }

            if (result.Length > 0)
            {
                if (clips[ClipType.ClockGameSplit] != default)
                {
                    result.Append(clips[ClipType.ClockGameSplit].Value);
                }
                else
                {
                    result.Append(GameClockSplitterDefault);
                }
            }

            if (clips[ClipType.ClockGameSec1] != default)
            {
                result.Append(clips[ClipType.ClockGameSec1].Value);
            }
            if (clips[ClipType.ClockGameSec2] != default)
            {
                result.Append(clips[ClipType.ClockGameSec2].Value);
            }

            return result.ToString();
        }

        private string GetClockShot()
        {
            var result = new StringBuilder();

            if (clips[ClipType.ClockShot1] != default)
            {
                result.Append(clips[ClipType.ClockShot1].Value);
            }

            if (clips[ClipType.ClockShot2] != default)
            {
                result.Append(clips[ClipType.ClockShot2].Value);
            }

            return result.ToString();
        }

        private string GetScoreGuest()
        {
            var result = new StringBuilder();

            if (clips[ClipType.ScoreGuest1] != default)
            {
                result.Append(clips[ClipType.ScoreGuest1].Value);
            }

            if (clips[ClipType.ScoreGuest2] != default)
            {
                result.Append(clips[ClipType.ScoreGuest2].Value);
            }

            if (clips[ClipType.ScoreGuest3] != default)
            {
                result.Append(clips[ClipType.ScoreGuest3].Value);
            }

            return result.ToString();
        }

        private string GetScoreHome()
        {
            var result = new StringBuilder();

            if (clips[ClipType.ScoreHome1] != default)
            {
                result.Append(clips[ClipType.ScoreHome1].Value);
            }

            if (clips[ClipType.ScoreHome2] != default)
            {
                result.Append(clips[ClipType.ScoreHome2].Value);
            }

            if (clips[ClipType.ScoreHome3] != default)
            {
                result.Append(clips[ClipType.ScoreHome3].Value);
            }

            return result.ToString();
        }

        private IEnumerable<bool> GetTickersUpToDate()
        {
            for (var index = 0; index < tickers.Count(); index++)
            {
                var result = settings.Scoreboard.Tickers[index].Item2 == tickers.ElementAt(index).Item2
                    && (!settings.Scoreboard.Tickers[index].Item2 || settings.Scoreboard.Tickers[index].Item1 == tickers.ElementAt(index).Item1);

                yield return result;
            }
        }

        private void UpdateBoard()
        {
            ClockGame = GetClockGame();
            clockGame = !ClockNotFromClip
                ? ClockGame
                : default;

            ClockShot = GetClockShot();
            clockShot = !ShotNotFromClip
                ? ClockShot
                : default;

            if (!PeriodNotFromClip
                && clips[ClipType.Period] != default)
            {
                Period = clips[ClipType.Period]?.Value;
                period = Period;
            }

            if (!ScoreNotFromClip)
            {
                ScoreHome = GetScoreHome();
                scoreHome = ScoreHome;

                ScoreGuest = GetScoreGuest();
                scoreGuest = ScoreGuest;
            }

            var frequencyTime = new TimeSpan(
                hours: 0,
                minutes: 0,
                seconds: TickersFrequency);

            if ((ticker == default && tickers?.Any() == true)
                || (ticker != default && tickers?.Any() != true)
                || !tickerLastUpdate.HasValue
                || tickerLastUpdate.Value.Add(frequencyTime) < DateTime.Now)
            {
                UpdateTicker();
            }

            var board = GetBoard();

            Message = JsonSerializer.Serialize(
                value: board,
                options: serializeOptions);

            eventAggregator
                .GetEvent<ScoreboardUpdatedEvent>()
                .Publish(Message);
        }

        private void UpdateTicker()
        {
            var current = default(string);

            var relevants = tickers
                .Where(t => t.Item2).ToArray();

            if (relevants?.Any() == true)
            {
                if (string.IsNullOrEmpty(ticker) || ++tickersInd >= relevants.Length)
                {
                    tickersInd = 0;
                }

                current = relevants[tickersInd].Item1;
            }

            if (current != ticker)
            {
                ticker = current;
            }

            tickerLastUpdate = DateTime.Now;
        }

        #endregion Private Methods
    }
}