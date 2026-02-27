using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class Server
    {
        #region Private Fields

        private int delaySocket = Defaults.DelaySocket;
        private int portServer = Defaults.PortServer;
        private int portSocket = Defaults.PortSocket;

        #endregion Private Fields

        #region Public Properties

        public int DelaySocket
        {
            get => delaySocket;
            set => delaySocket = value < Constants.DelayMin || value > Constants.DelayMax
                ? Defaults.DelaySocket
                : value;
        }

        public int PortServer
        {
            get => portServer;
            set => portServer = value < Constants.PortMin || value > Constants.PortMax
                ? Defaults.PortServer
                : value;
        }

        public int PortSocket
        {
            get => portSocket;
            set => portSocket = value < Constants.PortMin || value > Constants.PortMax
                ? Defaults.PortSocket
                : value;
        }

        #endregion Public Properties
    }
}