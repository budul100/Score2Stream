﻿using Avalonia.Media;
using Prism.Events;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Graphics;
using Score2Stream.Core.Events.Scoreboard;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models;
using Score2Stream.Core.Models.Content;
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
        private const int TickerFrequencyDefault = 30;

        private readonly IDictionary<ClipType, Clip> clips = new Dictionary<ClipType, Clip>();
        private readonly IEventAggregator eventAggregator;
        private readonly JsonSerializerOptions serializeOptions;

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
        private HashSet<string> tickers;
        private int tickersInd;

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
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            // Send first message to keep the web socket running
            eventAggregator.GetEvent<ServerStartedEvent>().Subscribe(
                action: UpdateBoard,
                keepSubscriberReferenceAlive: true);

            ColorHome = Colors.Yellow;
            ColorGuest = Colors.Blue;
            TickersFrequency = TickerFrequencyDefault;

            InitializeClips();
            Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public string ClockGame { get; private set; }

        public bool ClockGameIsUpToDate => ClockGame == clockGame;

        public bool ClockNotFromClip { get; set; }

        public string ClockShot { get; private set; }

        public bool ClockShotIsUpToDate => ClockShot == clockShot;

        public Color ColorGuest { get; set; }

        public bool ColorGuestUpToDate => ColorGuest == colorGuest;

        public Color ColorHome { get; set; }

        public bool ColorHomeUpToDate => ColorHome == colorHome;

        public bool IsGameOver { get; set; }

        public bool IsGameOverUpToDate => IsGameOver == isGameOver;

        public string Message { get; private set; }

        public string Period { get; set; }

        public bool PeriodNotFromClip { get; set; }

        public string Periods { get; set; }

        public bool PeriodsUpToDate => Periods == periods;

        public bool PeriodUpToDate => Period == period;

        public string ScoreGuest { get; set; }

        public bool ScoreGuestUpToDate => ScoreGuest == scoreGuest;

        public string ScoreHome { get; set; }

        public bool ScoreHomeUpToDate => ScoreHome == scoreHome;

        public bool ScoreNotFromClip { get; set; }

        public bool ShotNotFromClip { get; set; }

        public string TeamGuest { get; set; }

        public bool TeamGuestUpToDate => TeamGuest == teamGuest;

        public string TeamHome { get; set; }

        public bool TeamHomeUpToDate => TeamHome == teamHome;

        public IEnumerable<string> Tickers { get; set; }

        public int TickersFrequency { get; set; }

        public bool TickersUpToDate => (tickers?.Any() != true && Tickers?.Any() != true)
            || (tickers?.Any() == true && Tickers?.Any() == true && tickers.SetEquals(Tickers));

        public bool UpToDate => ColorGuestUpToDate
            && ColorHomeUpToDate
            && IsGameOverUpToDate
            && PeriodsUpToDate
            && PeriodUpToDate
            && ScoreGuestUpToDate
            && ScoreHomeUpToDate
            && TeamGuestUpToDate
            && TeamHomeUpToDate
            && TickersUpToDate;

        #endregion Public Properties

        #region Public Methods

        public void Set(Clip clip, ClipType clipType)
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
                || clips[ClipType.ScoreHome] == default
                || clips[ClipType.ScoreGuest] == default)
            {
                scoreHome = ScoreHome;
                scoreGuest = ScoreGuest;
            }

            this.tickers = Tickers?.ToHashSet();
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

        private void InitializeClips()
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

            if (!ScoreNotFromClip
                && clips[ClipType.ScoreHome] != default
                && clips[ClipType.ScoreGuest] != default)
            {
                ScoreHome = clips[ClipType.ScoreHome]?.Value ?? "0";
                scoreHome = ScoreHome;

                ScoreGuest = clips[ClipType.ScoreGuest]?.Value ?? "0";
                scoreGuest = ScoreGuest;
            }

            var frequencyTime = new TimeSpan(
                hours: 0,
                minutes: 0,
                seconds: TickersFrequency);

            if ((ticker == default && tickers?.Any() == true)
                || (ticker != default && tickers?.Any() != true)
                || !tickerLastUpdate.HasValue || tickerLastUpdate.Value.Add(frequencyTime) < DateTime.Now)
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

            if (tickers?.Any() == true)
            {
                if (string.IsNullOrEmpty(ticker) || ++tickersInd >= tickers.Count)
                {
                    tickersInd = 0;
                }

                current = tickers.ElementAt(tickersInd);
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