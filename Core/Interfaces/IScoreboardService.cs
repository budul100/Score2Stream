﻿using Core.Enums;
using Core.Models;
using System.Collections.Generic;

namespace Core.Interfaces
{
    public interface IScoreboardService
    {
        #region Public Properties

        bool ClockNotFromClip { get; set; }

        bool HasUpdates { get; }

        string Message { get; }

        bool PeriodNotFromClip { get; set; }

        bool ScoreNotFromClip { get; set; }

        bool ShotNotFromClip { get; set; }

        #endregion Public Properties

        #region Public Methods

        void Announce();

        void SetClip(ClipType contentType, Clip clip);

        void Update(string period, int? periods, bool isGameOver, string teamHome, string teamGuest,
            int scoreHome, int scoreGuest, string colorHome, string colorGuest, IEnumerable<string> tickers);

        #endregion Public Methods
    }
}