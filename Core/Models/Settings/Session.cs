using System.Collections.Generic;

namespace Score2Stream.Core.Models.Settings
{
    public class Session
    {
        #region Public Properties

        public App App { get; set; } = new App();

        public Detection Detection { get; set; } = new Detection();

        public List<Contents.Input> Inputs { get; set; } = new List<Contents.Input>();

        public Scoreboard Scoreboard { get; set; } = new Scoreboard();

        public Video Video { get; set; } = new Video();

        #endregion Public Properties
    }
}