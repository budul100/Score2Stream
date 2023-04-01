using System.Text.Json.Serialization;

namespace Core.Models.Sender
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