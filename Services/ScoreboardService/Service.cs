using Core.Enums;
using Core.Events.Clip;
using Core.Events.Graphics;
using Core.Events.Scoreboard;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using Core.Models.Sender;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ScoreboardService
{
    public class Service
        : IScoreboardService
    {
        #region Private Fields

        private const char GameClockSplitterDefault = ':';

        private readonly IDictionary<ClipType, Clip> clips = new Dictionary<ClipType, Clip>();
        private readonly IEventAggregator eventAggregator;
        private readonly JsonSerializerOptions serializeOptions;

        private string colorGuest;
        private string colorHome;
        private bool isGameOver;
        private string period;
        private int? periods;
        private int scoreGuest;
        private int scoreHome;
        private string teamGuest;
        private string teamHome;
        private IEnumerable<string> tickers;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: UpdateMessage,
                keepSubscriberReferenceAlive: true);

            // Send first message to keep the web socket running
            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: UpdateMessage,
                keepSubscriberReferenceAlive: true);

            InitializeContents();
        }

        #endregion Public Constructors

        #region Public Properties

        public bool ClockNotFromClip { get; set; }

        public bool HasUpdates { get; private set; }

        public string Message { get; private set; }

        public bool PeriodNotFromClip { get; set; }

        public bool ScoreNotFromClip { get; set; }

        public bool ShotNotFromClip { get; set; }

        #endregion Public Properties

        #region Public Methods

        public void Announce()
        {
            HasUpdates = true;

            eventAggregator
                .GetEvent<ScoreboardAnnouncedEvent>()
                .Publish();
        }

        public void SetClip(ClipType clipType, Clip clip)
        {
            if (clipType != clip.Type)
            {
                if (clip.Type != ClipType.None
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

                eventAggregator
                    .GetEvent<ClipUpdatedEvent>()
                    .Publish(clip);
            }
        }

        public void Update(string period, int? periods, bool isGameOver, string teamHome, string teamGuest,
            int scoreHome, int scoreGuest, string colorHome, string colorGuest, IEnumerable<string> tickers)
        {
            this.period = period;
            this.periods = periods;
            this.isGameOver = isGameOver;
            this.teamHome = teamHome;
            this.teamGuest = teamGuest;
            this.scoreHome = scoreHome;
            this.scoreGuest = scoreGuest;
            this.colorHome = colorHome;
            this.colorGuest = colorGuest;
            this.tickers = tickers;

            UpdateMessage();

            HasUpdates = false;

            eventAggregator
                .GetEvent<ScoreboardAnnouncedEvent>()
                .Publish();
        }

        #endregion Public Methods

        #region Private Methods

        private Board GetBoard()
        {
            var clock = !ClockNotFromClip
                ? GetClockGame()
                : default;
            var shot = !ShotNotFromClip
                ? GetClockShot()
                : default;

            var currentPeriod = !PeriodNotFromClip
                ? clips[ClipType.Period]?.Value
                : period;
            var currentPeriods = periods?.ToString();

            var game = new Game
            {
                Clock = clock,
                Possesion = default,
                Period = currentPeriod,
                Periods = currentPeriods,
                Shot = shot,
            };

            var currentScoreHome = !ScoreNotFromClip
                ? clips[ClipType.ScoreHome]?.Value
                : scoreHome.ToString();

            var home = new Home
            {
                Color = colorHome,
                Fouls = default,
                ImagePath = default,
                Name = teamHome,
                Score = currentScoreHome,
            };

            var currentScoreGuest = !ScoreNotFromClip
                ? clips[ClipType.ScoreGuest]?.Value
                : scoreGuest.ToString();

            var guest = new Guest
            {
                Color = colorGuest,
                Fouls = default,
                ImagePath = default,
                Name = teamGuest,
                Score = currentScoreGuest,
            };

            var result = new Board
            {
                Game = game,
                Guest = guest,
                Home = home,
                GameID = default,
                GameOver = isGameOver,
                Ticker = default,
            };

            return result;
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

            if (clips[ClipType.ClockGameSplit] != default)
            {
                result.Append(clips[ClipType.ClockShotSplit].Value);
            }

            if (clips[ClipType.ClockShot2] != default)
            {
                result.Append(clips[ClipType.ClockShot2].Value);
            }

            return result.ToString();
        }

        private void InitializeContents()
        {
            var relevants = Enum.GetValues(typeof(ClipType))
                .OfType<ClipType>()
                .Where(t => t != ClipType.None).ToArray();

            foreach (var relevant in relevants)
            {
                clips.Add(
                    key: relevant,
                    value: default);
            }
        }

        private void UpdateMessage()
        {
            var board = GetBoard();

            Message = JsonSerializer.Serialize(
                value: board,
                options: serializeOptions);

            eventAggregator
                .GetEvent<ScoreboardUpdatedEvent>()
                .Publish(Message);
        }

        #endregion Private Methods
    }
}