using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Score2Stream.Core.Settings
{
    public class UserSettings
    {
        #region Private Fields

        private const int ImagesQueueSizeDefault = 3;
        private const int ProcessingDelayDefault = 100;
        private const int ThresholdDetectingDefault = 90;
        private const int ThresholdMatchingDefault = 40;
        private const int TickersFrequencyDefault = 10;
        private const int TickersSize = 6;
        private const int WaitingDurationDefault = 100;

        private (string, bool)[] tickers = Array.Empty<(string, bool)>();

        #endregion Private Fields

        #region Public Constructors

        public UserSettings()
        {
            InitializeTickers();
        }

        #endregion Public Constructors

        #region Public Properties

        public string ColorGuest { get; set; } = Colors.Yellow.ToString();

        public string ColorHome { get; set; } = Colors.Blue.ToString();

        public string FilePathVideo { get; set; }

        public int ImagesQueueSize { get; set; } = ImagesQueueSizeDefault;

        public bool NoCentering { get; set; }

        public int ProcessingDelay { get; set; } = ProcessingDelayDefault;

        public string TeamGuest { get; set; }

        public string TeamHome { get; set; }

        public int ThresholdDetecting { get; set; } = ThresholdDetectingDefault;

        public int ThresholdMatching { get; set; } = ThresholdMatchingDefault;

        [JsonIgnore]
        public (string, bool)[] Tickers
        {
            get
            {
                return tickers;
            }
            set
            {
                tickers = value;
                InitializeTickers();
            }
        }

        public int TickersFrequency { get; set; } = TickersFrequencyDefault;

        public List<Tuple<string, bool>> TickersList
        {
            get
            {
                var result = new List<Tuple<string, bool>>();
                foreach (var ticker in tickers)
                {
                    result.Add(new Tuple<string, bool>(ticker.Item1, ticker.Item2));
                }

                return result;
            }
            set
            {
                var result = new List<(string, bool)>();
                foreach (var ticker in value)
                {
                    result.Add((ticker.Item1, ticker.Item2));
                }

                Tickers = result.ToArray();
            }
        }

        public int WaitingDuration { get; set; } = WaitingDurationDefault;

        #endregion Public Properties

        #region Private Methods

        private void InitializeTickers()
        {
            if (Tickers.Length != TickersSize)
            {
                Array.Resize(
                    ref tickers,
                    TickersSize);
            }
        }

        #endregion Private Methods
    }
}