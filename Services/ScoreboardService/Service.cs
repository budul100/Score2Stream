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
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Events.Segment;
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
        private readonly ScoreboardUpdatedEvent scoreboardUpdatedEvent;
        private readonly SegmentModifiedEvent segmentModifiedEvent;
        private readonly Dictionary<SegmentType, Segment> segments = [];
        private readonly JsonSerializerOptions serializeOptions;
        private readonly ISettingsService<Session> settingsService;

        private string clockGame;
        private string clockShot;
        private Color colorGuest;
        private Color colorHome;
        private string foulsGuest;
        private string foulsHome;
        private bool isGameOver;
        private string period;
        private string periods;
        private string scoreGuest;
        private string scoreHome;
        private string secondsLast;
        private DateTime secondsUpdate;
        private string shotLast;
        private string teamGuest;
        private string teamHome;
        private string ticker;
        private IEnumerable<(string, bool)> tickers;
        private int tickersInd;
        private DateTime? tickersUpdate;

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
            segmentModifiedEvent = eventAggregator.GetEvent<SegmentModifiedEvent>();
            scoreboardUpdatedEvent = eventAggregator.GetEvent<ScoreboardUpdatedEvent>();

            // Send first message to keep the web socket running
            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<InputUpdatedEvent>().Subscribe(
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            segments = Commons.Extensions.EnumExtensions.GetValues<SegmentType>()
                .Where(t => t != SegmentType.None).ToDictionary(
                    keySelector: t => t,
                    elementSelector: _ => default(Segment));

            Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public string ClockGame { get; private set; }

        public bool ClockGameIsUpToDate => ClockGame == clockGame;

        public bool ClockNotDetected { get; set; }

        public string ClockShot { get; private set; }

        public bool ClockShotIsUpToDate => ClockShot == clockShot;

        public bool ClockWithTenthSec
        {
            get { return settingsService.Contents?.Scoreboard.ClockWithTenthSec ?? false; }
            set
            {
                if (settingsService.Contents.Scoreboard.ClockWithTenthSec != value)
                {
                    settingsService.Contents.Scoreboard.ClockWithTenthSec = value;
                    settingsService.Save();
                }
            }
        }

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

        public string FoulsGuest { get; set; }

        public bool FoulsGuestUpToDate => FoulsGuest == foulsGuest;

        public string FoulsHome { get; set; }

        public bool FoulsHomeUpToDate => FoulsHome == foulsHome;

        public bool FoulsNotDetected { get; set; }

        public bool IsGameOver { get; set; }

        public bool IsGameOverUpToDate => IsGameOver == isGameOver;

        public bool IsUpToDate => IsGameOverUpToDate
            && ColorGuestUpToDate
            && ColorHomeUpToDate
            && FoulsGuestUpToDate
            && FoulsHomeUpToDate
            && PeriodsUpToDate
            && PeriodUpToDate
            && ScoreGuestUpToDate
            && ScoreHomeUpToDate
            && TeamGuestUpToDate
            && TeamHomeUpToDate
            && TickersUpToDate.All(t => t);

        public string Message { get; private set; }

        public string Period { get; set; }

        public bool PeriodNotDetected { get; set; }

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

        public bool ScoreNotDetected { get; set; }

        public bool ShotNotDetected { get; set; }

        public bool ShotWithTenthSec
        {
            get { return settingsService.Contents?.Scoreboard.ShotWithTenthSec ?? false; }
            set
            {
                if (settingsService.Contents.Scoreboard.ShotWithTenthSec != value)
                {
                    settingsService.Contents.Scoreboard.ShotWithTenthSec = value;
                    settingsService.Save();
                }
            }
        }

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

        #endregion Public Properties

        #region Public Methods

        public void Bind(Area area, AreaType type)
        {
            if (area != default)
            {
                ReleaseArea(area);

                if (type != AreaType.None)
                {
                    var segmentTypes = type
                        .GetSegmentTypes().ToArray();

                    if (area.Size != segmentTypes.Length)
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

                    var releasedAreas = segments
                        .Where(c => c.Value != default
                            && c.Value?.Area != area
                            && segmentTypes.Contains(c.Key))
                        .Select(c => c.Value.Area)
                        .Distinct().ToArray();

                    foreach (var releasedArea in releasedAreas)
                    {
                        ReleaseArea(releasedArea);
                    }

                    for (var index = 0; index < area.Size; index++)
                    {
                        var segment = area.Segments.ElementAt(index);

                        segments[segmentTypes[index]] = segment;

                        if (segment.Type != segmentTypes[index])
                        {
                            segment.Type = segmentTypes[index];
                            segmentModifiedEvent.Publish(segment);
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

                var releasedSegments = segments
                    .Where(c => area.Segments.Contains(c.Value)).ToArray();

                foreach (var releaseSegment in releasedSegments)
                {
                    segments[releaseSegment.Key] = default;

                    if (releaseSegment.Value.Type != SegmentType.None)
                    {
                        releaseSegment.Value.Type = SegmentType.None;
                        segmentModifiedEvent.Publish(releaseSegment.Value);
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

            if (PeriodNotDetected
                || segments[SegmentType.Period] == default)
            {
                period = Period;
            }

            if (ScoreNotDetected
                || segments[SegmentType.ScoreHome1] == default
                || segments[SegmentType.ScoreHome2] == default
                || segments[SegmentType.ScoreHome3] == default
                || segments[SegmentType.ScoreGuest1] == default
                || segments[SegmentType.ScoreGuest2] == default
                || segments[SegmentType.ScoreGuest3] == default)
            {
                scoreHome = ScoreHome;
                scoreGuest = ScoreGuest;
            }

            if (FoulsNotDetected
                || segments[SegmentType.FoulsHome] == default
                || segments[SegmentType.FoulsGuest] == default)
            {
                foulsHome = FoulsHome;
                foulsGuest = FoulsGuest;
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
                Fouls = foulsHome,
                ImagePath = default,
                Name = teamHome,
                Score = scoreHome,
            };

            var guest = new Guest
            {
                Color = colorGuest.GetColorHex(),
                Fouls = foulsGuest,
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

            if (!string.IsNullOrWhiteSpace(segments[SegmentType.ClockGameMin1]?.Value))
            {
                result.Append(segments[SegmentType.ClockGameMin1].Value);
            }
            if (!string.IsNullOrWhiteSpace(segments[SegmentType.ClockGameMin2]?.Value))
            {
                result.Append(segments[SegmentType.ClockGameMin2].Value);
            }

            if (result.Length > 0)
            {
                if (segments[SegmentType.ClockGameSplit] != default)
                {
                    result.Append(segments[SegmentType.ClockGameSplit].Value);
                }
                else
                {
                    result.Append(Constants.GameClockSplitterDefault);
                }
            }

            var seconds = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(segments[SegmentType.ClockGameSec1]?.Value))
            {
                seconds.Append(segments[SegmentType.ClockGameSec1].Value);
            }
            if (!string.IsNullOrWhiteSpace(segments[SegmentType.ClockGameSec2]?.Value))
            {
                seconds.Append(segments[SegmentType.ClockGameSec2].Value);
            }

            var currentTime = DateTime.Now;

            if (ClockWithTenthSec
                || seconds.Length > 1
                || currentTime >= secondsUpdate.AddSeconds(1))
            {
                result.Append(seconds);
            }

            if (string.IsNullOrWhiteSpace(secondsLast)
                || seconds.Length == 0
                || secondsLast != seconds.ToString())
            {
                secondsUpdate = currentTime;
            }

            secondsLast = seconds.ToString();

            return result.ToString();
        }

        private string GetClockShot()
        {
            var result = new StringBuilder();

            var shot = new StringBuilder();

            if (segments[SegmentType.ClockShot1] != default)
            {
                shot.Append(segments[SegmentType.ClockShot1].Value);
            }

            if (segments[SegmentType.ClockShot2] != default)
            {
                shot.Append(segments[SegmentType.ClockShot2].Value);
            }

            var currentTime = DateTime.Now;

            if (ClockWithTenthSec
                || shot.Length > 1
                || currentTime >= secondsUpdate.AddSeconds(1))
            {
                result.Append(shot);
            }

            if (string.IsNullOrWhiteSpace(shotLast)
                || shot.Length == 0
                || shotLast != shot.ToString())
            {
                secondsUpdate = currentTime;
            }

            shotLast = shot.ToString();

            return result.ToString();
        }

        private string GetFoulsGuest()
        {
            return segments[SegmentType.FoulsGuest]?.Value;
        }

        private string GetFoulsHome()
        {
            return segments[SegmentType.FoulsHome]?.Value;
        }

        private string GetPeriod()
        {
            return segments[SegmentType.Period]?.Value;
        }

        private string GetScoreGuest()
        {
            var result = new StringBuilder();

            if (segments[SegmentType.ScoreGuest1] != default)
            {
                result.Append(segments[SegmentType.ScoreGuest1].Value);
            }

            if (segments[SegmentType.ScoreGuest2] != default)
            {
                result.Append(segments[SegmentType.ScoreGuest2].Value);
            }

            if (segments[SegmentType.ScoreGuest3] != default)
            {
                result.Append(segments[SegmentType.ScoreGuest3].Value);
            }

            return result.ToString();
        }

        private string GetScoreHome()
        {
            var result = new StringBuilder();

            if (segments[SegmentType.ScoreHome1] != default)
            {
                result.Append(segments[SegmentType.ScoreHome1].Value);
            }

            if (segments[SegmentType.ScoreHome2] != default)
            {
                result.Append(segments[SegmentType.ScoreHome2].Value);
            }

            if (segments[SegmentType.ScoreHome3] != default)
            {
                result.Append(segments[SegmentType.ScoreHome3].Value);
            }

            return result.ToString();
        }

        private IEnumerable<bool> GetTickersUpToDate()
        {
            if (tickers?.Any() == true)
            {
                var current = tickers.ToArray();

                var settingsTickers = settingsService.Contents.Scoreboard.Tickers;

                for (var index = 0; index < current.Length; index++)
                {
                    var result = settingsTickers[index].Item2 == current[index].Item2
                        && (!settingsTickers[index].Item2 || settingsTickers[index].Item1 == current[index].Item1);

                    yield return result;
                }
            }
        }

        private void UpdateBoard()
        {
            ClockGame = GetClockGame();
            clockGame = !ClockNotDetected && !isGameOver
                ? ClockGame
                : default;

            ClockShot = GetClockShot();
            clockShot = !ShotNotDetected && !isGameOver
                ? ClockShot
                : default;

            if (!PeriodNotDetected
                && !isGameOver)
            {
                Period = GetPeriod();
                period = Period;
            }

            if (!ScoreNotDetected
                && !isGameOver)
            {
                ScoreHome = GetScoreHome();
                scoreHome = ScoreHome;

                ScoreGuest = GetScoreGuest();
                scoreGuest = ScoreGuest;
            }

            if (!FoulsNotDetected
                && !isGameOver)
            {
                FoulsHome = GetFoulsHome();
                foulsHome = FoulsHome;

                FoulsGuest = GetFoulsGuest();
                foulsGuest = FoulsGuest;
            }

            var frequencyTime = new TimeSpan(
                hours: 0,
                minutes: 0,
                seconds: TickersFrequency);

            if ((ticker == default && tickers?.Any() == true)
                || (ticker != default && tickers?.Any() != true)
                || !tickersUpdate.HasValue
                || tickersUpdate.Value.Add(frequencyTime) < DateTime.Now)
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

                if (relevants?.Length > 0)
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

                tickersUpdate = DateTime.Now;
            }
        }

        #endregion Private Methods
    }
}