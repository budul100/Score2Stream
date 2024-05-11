using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Avalonia.Media;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Events.Video;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Scoreboard;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.ScoreboardService.Extensions;

namespace Score2Stream.ScoreboardService
{
    public class Service
        : IScoreboardService
    {
        #region Private Fields

        private readonly AreaModifiedEvent areaModifiedEvent;
        private readonly SegmentModifiedEvent clipModifiedEvent;
        private readonly Dictionary<SegmentType, Segment> clips = new();
        private readonly ScoreboardUpdatedEvent scoreboardUpdatedEvent;
        private readonly JsonSerializerOptions serializeOptions;
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

            serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            areaModifiedEvent = eventAggregator.GetEvent<AreaModifiedEvent>();
            clipModifiedEvent = eventAggregator.GetEvent<SegmentModifiedEvent>();
            scoreboardUpdatedEvent = eventAggregator.GetEvent<ScoreboardUpdatedEvent>();

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            // Send first message to keep the web socket running
            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            clips = Commons.Extensions.EnumExtensions.GetValues<SegmentType>()
                .Where(t => t != SegmentType.None).ToDictionary(
                    keySelector: t => t,
                    elementSelector: _ => default(Segment));

            Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public string ClockGame { get; private set; }

        public bool ClockGameIsUpToDate => ClockGame == clockGame;

        public bool ClockNotFromClip { get; set; }

        public string ClockShot { get; private set; }

        public bool ClockShotIsUpToDate => ClockShot == clockShot;

        public Color ColorGuest
        {
            get
            {
                if (Color.TryParse(
                    s: settingsService.Contents?.Scoreboard.ColorGuest,
                    color: out var result))
                {
                    return result;
                }
                else
                {
                    return default;
                }
            }
            set
            {
                var color = value.ToString();

                if (color != settingsService.Contents.Scoreboard.ColorGuest)
                {
                    settingsService.Contents.Scoreboard.ColorGuest = color;
                    settingsService.Save();
                }
            }
        }

        public bool ColorGuestUpToDate => ColorGuest == colorGuest;

        public Color ColorHome
        {
            get
            {
                if (Color.TryParse(
                    s: settingsService.Contents?.Scoreboard.ColorHome,
                    color: out var result))
                {
                    return result;
                }
                else
                {
                    return default;
                }
            }
            set
            {
                var color = value.ToString();

                if (color != settingsService.Contents.Scoreboard.ColorHome)
                {
                    settingsService.Contents.Scoreboard.ColorHome = color;
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
            get { return settingsService.Contents?.Scoreboard.Periods; }
            set
            {
                if (value != settingsService.Contents.Scoreboard.Periods)
                {
                    settingsService.Contents.Scoreboard.Periods = value;
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
            get { return settingsService.Contents?.Scoreboard.TeamGuest; }
            set
            {
                if (value != settingsService.Contents.Scoreboard.TeamGuest)
                {
                    settingsService.Contents.Scoreboard.TeamGuest = value;
                    settingsService.Save();
                }
            }
        }

        public bool TeamGuestUpToDate => TeamGuest == teamGuest;

        public string TeamHome
        {
            get { return settingsService.Contents?.Scoreboard.TeamHome; }
            set
            {
                if (value != settingsService.Contents.Scoreboard.TeamHome)
                {
                    settingsService.Contents.Scoreboard.TeamHome = value;
                    settingsService.Save();
                }
            }
        }

        public bool TeamHomeUpToDate => TeamHome == teamHome;

        public (string, bool)[] Tickers => settingsService.Contents?.Scoreboard.Tickers;

        public int TickersFrequency
        {
            get { return settingsService.Contents?.Scoreboard.TickersFrequency ?? 0; }
            set
            {
                if (settingsService.Contents.Scoreboard.TickersFrequency != value)
                {
                    settingsService.Contents.Scoreboard.TickersFrequency = value;

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

        public void BindArea(Area area, AreaType type)
        {
            if (area != default)
            {
                if (type == AreaType.None)
                {
                    ReleaseArea(area);
                }
                else
                {
                    var clipTypes = type
                        .GetClipTypes().ToArray();

                    if (area.Size != clipTypes.Length)
                    {
                        throw new ArgumentException(
                            message: $"The area type {type} does not fit the area size {area.Size}.",
                            paramName: nameof(type));
                    }

                    if (area.Type != type)
                    {
                        area.Type = type;

                        areaModifiedEvent.Publish(area);
                    }

                    var releasedAreas = clips
                        .Where(c => c.Value != default
                            && c.Value?.Area != area
                            && clipTypes.Contains(c.Key))
                        .Select(c => c.Value.Area)
                        .Distinct().ToArray();

                    foreach (var releasedArea in releasedAreas)
                    {
                        ReleaseArea(releasedArea);
                    }

                    for (var index = 0; index < area.Size; index++)
                    {
                        var clip = area.Segments.ElementAt(index);

                        clips[clipTypes[index]] = clip;

                        if (clip.Type != clipTypes[index])
                        {
                            clip.Type = clipTypes[index];

                            clipModifiedEvent.Publish(clip);
                        }
                    }
                }
            }
        }

        public void ReleaseArea(Area area)
        {
            if (area != default)
            {
                if (area.Type != AreaType.None)
                {
                    area.Type = AreaType.None;

                    areaModifiedEvent.Publish(area);
                }

                var releasedClips = clips
                    .Where(c => area.Segments.Contains(c.Value))
                    .Distinct().ToArray();

                foreach (var releasedClip in releasedClips)
                {
                    clips[releasedClip.Key] = default;

                    if (releasedClip.Value.Type != SegmentType.None)
                    {
                        releasedClip.Value.Type = SegmentType.None;

                        clipModifiedEvent.Publish(releasedClip.Value);
                    }
                }
            }
        }

        public void SetTicker(int number, string text)
        {
            if (settingsService.Contents.Scoreboard.Tickers.Length > number)
            {
                settingsService.Contents.Scoreboard.Tickers[number].Item1 = text;
                settingsService.Save();

                TickersUpToDate = GetTickersUpToDate().ToArray();
            }
        }

        public void SetTicker(int number, bool isActive)
        {
            if (settingsService.Contents.Scoreboard.Tickers.Length > number)
            {
                settingsService.Contents.Scoreboard.Tickers[number].Item2 = isActive;
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
                || clips[SegmentType.Period] == default)
            {
                period = Period;
            }

            if (ScoreNotFromClip
                || clips[SegmentType.ScoreHome1] == default
                || clips[SegmentType.ScoreHome2] == default
                || clips[SegmentType.ScoreHome3] == default
                || clips[SegmentType.ScoreGuest1] == default
                || clips[SegmentType.ScoreGuest2] == default
                || clips[SegmentType.ScoreGuest3] == default)
            {
                scoreHome = ScoreHome;
                scoreGuest = ScoreGuest;
            }

            tickers = Tickers?.ToArray();
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

        private string GetClockGame()
        {
            var result = new StringBuilder();

            if (clips[SegmentType.ClockGameMin1] != default)
            {
                result.Append(clips[SegmentType.ClockGameMin1].Value);
            }
            if (clips[SegmentType.ClockGameMin2] != default)
            {
                result.Append(clips[SegmentType.ClockGameMin2].Value);
            }

            if (result.Length > 0)
            {
                if (clips[SegmentType.ClockGameSplit] != default)
                {
                    result.Append(clips[SegmentType.ClockGameSplit].Value);
                }
                else
                {
                    result.Append(Constants.GameClockSplitterDefault);
                }
            }

            if (clips[SegmentType.ClockGameSec1] != default)
            {
                result.Append(clips[SegmentType.ClockGameSec1].Value);
            }
            if (clips[SegmentType.ClockGameSec2] != default)
            {
                result.Append(clips[SegmentType.ClockGameSec2].Value);
            }

            return result.ToString();
        }

        private string GetClockShot()
        {
            var result = new StringBuilder();

            if (clips[SegmentType.ClockShot1] != default)
            {
                result.Append(clips[SegmentType.ClockShot1].Value);
            }

            if (clips[SegmentType.ClockShot2] != default)
            {
                result.Append(clips[SegmentType.ClockShot2].Value);
            }

            return result.ToString();
        }

        private string GetScoreGuest()
        {
            var result = new StringBuilder();

            if (clips[SegmentType.ScoreGuest1] != default)
            {
                result.Append(clips[SegmentType.ScoreGuest1].Value);
            }

            if (clips[SegmentType.ScoreGuest2] != default)
            {
                result.Append(clips[SegmentType.ScoreGuest2].Value);
            }

            if (clips[SegmentType.ScoreGuest3] != default)
            {
                result.Append(clips[SegmentType.ScoreGuest3].Value);
            }

            return result.ToString();
        }

        private string GetScoreHome()
        {
            var result = new StringBuilder();

            if (clips[SegmentType.ScoreHome1] != default)
            {
                result.Append(clips[SegmentType.ScoreHome1].Value);
            }

            if (clips[SegmentType.ScoreHome2] != default)
            {
                result.Append(clips[SegmentType.ScoreHome2].Value);
            }

            if (clips[SegmentType.ScoreHome3] != default)
            {
                result.Append(clips[SegmentType.ScoreHome3].Value);
            }

            return result.ToString();
        }

        private IEnumerable<bool> GetTickersUpToDate()
        {
            if (tickers?.Any() == true)
            {
                for (var index = 0; index < tickers.Count(); index++)
                {
                    var result = settingsService.Contents.Scoreboard.Tickers[index].Item2 == tickers.ElementAt(index).Item2
                        && (!settingsService.Contents.Scoreboard.Tickers[index].Item2
                        || settingsService.Contents.Scoreboard.Tickers[index].Item1 == tickers.ElementAt(index).Item1);

                    yield return result;
                }
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
                && clips[SegmentType.Period] != default)
            {
                Period = clips[SegmentType.Period]?.Value;
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

            scoreboardUpdatedEvent.Publish(Message);
        }

        private void UpdateTicker()
        {
            if (tickers?.Any() == true)
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
        }

        #endregion Private Methods
    }
}