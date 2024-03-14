using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class Server
    {
        #region Public Properties

        public int PortServerHttp { get; set; } = Defaults.PortServerHttpDefault;

        public int PortSocketHttp { get; set; } = Defaults.PortSocketHttpDefault;

        public int WebSocketDelay { get; set; } = Defaults.WebSocketDelayDefault;

        #endregion Public Properties
    }
}