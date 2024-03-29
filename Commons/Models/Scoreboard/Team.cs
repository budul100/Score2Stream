﻿namespace Score2Stream.Commons.Models.Scoreboard
{
    public abstract class Team
    {
        #region Public Properties

        public string Color { get; set; }

        public int Fouls { get; set; }

        public string ImagePath { get; set; }

        public string Name { get; set; }

        public string Score { get; set; }

        #endregion Public Properties
    }
}