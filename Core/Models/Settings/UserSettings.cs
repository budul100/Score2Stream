using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;

namespace Score2Stream.Core.Models.Settings
{
    public class UserSettings
    {
        #region Public Properties

        public App App { get; set; } = new App();

        public Detection Detection { get; set; } = new Detection();

        public List<Input> Inputs { get; set; } = new List<Input>();

        public Scoreboard Scoreboard { get; set; } = new Scoreboard();

        public Session Session { get; set; } = new Session();

        #endregion Public Properties
    }
}