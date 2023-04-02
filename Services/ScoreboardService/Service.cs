using Core.Events;
using Core.Interfaces;
using Core.Models;
using Core.Models.Sender;
using Prism.Events;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ScoreboardService
{
    public class Service
        : IScoreboardService
    {
        #region Private Fields

        private readonly IDictionary<string, Clip> clips = new Dictionary<string, Clip>();
        private readonly IClipService clipService;
        private readonly IEventAggregator eventAggregator;
        private readonly JsonSerializerOptions serializeOptions;

        #endregion Private Fields

        #region Public Constructors

        public Service(IClipService clipService, IEventAggregator eventAggregator)
        {
            this.clipService = clipService;
            this.eventAggregator = eventAggregator;
            serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => OnClipUpdate(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<WebcamUpdatedEvent>().Subscribe(
                action: UpdateScoreboard,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<UpdateScoreboardEvent>().Subscribe(
                action: UpdateScoreboard,
                keepSubscriberReferenceAlive: true);
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

        #region Private Methods

        private Board GetBoard()
        {
            var clock = GetClock();

            var game = new Game
            {
                Clock = clock,
                Possesion = default,
                Quarter = Period,
                ShotClock = default,
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

        private string GetClock()
        {
            var result = new StringBuilder();

            if (clips.ContainsKey("Clip1"))
            {
                result.Append(clips["Clip1"].Value);
            }
            if (clips.ContainsKey("Clip2"))
            {
                result.Append(clips["Clip2"].Value);
            }

            if (result.Length > 0)
            {
                result.Append(":");
            }

            if (clips.ContainsKey("Clip3"))
            {
                result.Append(clips["Clip3"].Value);
            }
            if (clips.ContainsKey("Clip4"))
            {
                result.Append(clips["Clip4"].Value);
            }

            return result.ToString();
        }

        private void OnClipUpdate()
        {
            clips.Clear();

            foreach (var clip in clipService.Clips)
            {
                clips.Add(
                    key: clip.Name,
                    value: clip);
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