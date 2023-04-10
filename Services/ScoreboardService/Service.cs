using Core.Enums;
using Core.Events;
using Core.Events.Clip;
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
                action: UpdateScoreboard,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<UpdateScoreboardEvent>().Subscribe(
                action: UpdateScoreboard,
                keepSubscriberReferenceAlive: true);

            InitializeContents();
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsGameOver { get; set; }

        public string Message { get; private set; }

        public int Period { get; set; }

        public int ScoreGuest { get; set; }

        public int ScoreHome { get; set; }

        public string TeamGuest { get; set; }

        public string TeamHome { get; set; }

        public string Ticker { get; set; }

        #endregion Public Properties

        #region Public Methods

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

        #endregion Public Methods

        #region Private Methods

        private Board GetBoard()
        {
            var clock = GetClockGame();
            var shot = GetClockShot();

            var game = new Game
            {
                Clock = clock,
                Possesion = default,
                Quarter = Period,
                Shot = shot,
            };

            var home = new Home
            {
                Color = default,
                Fouls = default,
                ImagePath = default,
                Name = TeamHome,
                Score = ScoreHome.ToString(),
            };

            var guest = new Guest
            {
                Color = default,
                Fouls = default,
                ImagePath = default,
                Name = TeamGuest,
                Score = ScoreGuest.ToString(),
            };

            var result = new Board
            {
                Game = game,
                Guest = guest,
                Home = home,
                GameID = default,
                GameOver = false,
                Ticker = Ticker,
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

        private void UpdateScoreboard()
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