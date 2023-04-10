using System.Text.Json.Serialization;

namespace Core.Models.Sender
{
    public class Game
    {
        #region Public Properties

        public string Clock { get; set; }

        public string Period { get; set; }

        public string Periods { get; set; }

        public string Possesion { get; set; }

        [JsonPropertyName("shot_clock")]
        public string Shot { get; set; }

        #endregion Public Properties
    }
}