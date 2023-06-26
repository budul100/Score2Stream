using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Score2Stream.Core.Models.Settings
{
    public class Scoreboard
    {
        #region Private Fields

        private const int TickersFrequencyDefault = 10;
        private const int TickersSize = 6;

        private (string, bool)[] tickers = Array.Empty<(string, bool)>();

        #endregion Private Fields

        #region Public Constructors

        public Scoreboard()
        {
            InitializeTickers();
        }

        #endregion Public Constructors

        #region Public Properties

        public string ColorGuest { get; set; } = Colors.Yellow.ToString();

        public string ColorHome { get; set; } = Colors.Blue.ToString();

        public string TeamGuest { get; set; }

        public string TeamHome { get; set; }

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