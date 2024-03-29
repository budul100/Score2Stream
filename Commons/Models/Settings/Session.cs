﻿using System.Collections.Generic;

namespace Score2Stream.Commons.Models.Settings
{
    public class Session
    {
        #region Public Properties

        public App App { get; set; } = new App();

        public Detection Detection { get; set; } = new Detection();

        public List<Contents.Input> Inputs { get; set; } = new List<Contents.Input>();

        public Scoreboard Scoreboard { get; set; } = new Scoreboard();

        public Server Server { get; set; } = new Server();

        public Video Video { get; set; } = new Video();

        #endregion Public Properties
    }
}