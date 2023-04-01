using System.Text.Json.Serialization;

namespace Core.Models.Sender
{
    public class Game
    {
        #region Public Properties

        public string Clock { get; set; }

        public string Possesion { get; set; }

        public int Quarter { get; set; }

        [JsonPropertyName("shot_clock")]
        public string ShotClock { get; set; }

        #endregion Public Properties
    }
}