﻿namespace Score2Stream.Core.Settings
{
    public class UserSettings
    {
        #region Public Properties

        public Detection Detection { get; set; } = new Detection();

        public Scoreboard Scoreboard { get; set; } = new Scoreboard();

        public Video Video { get; set; } = new Video();

        #endregion Public Properties
    }
}