﻿using System.Text.Json.Serialization;

namespace Score2Stream.Commons.Models.Scoreboard
{
    public class Board
    {
        #region Public Properties

        public Game Game { get; set; }

        [JsonPropertyName("game_id")]
        public string GameID { get; set; }

        [JsonPropertyName("game_over")]
        public bool GameOver { get; set; }

        public Guest Guest { get; set; }

        public Home Home { get; set; }

        public string Ticker { get; set; }

        #endregion Public Properties
    }
}