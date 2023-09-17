using Avalonia.Media;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;

namespace Score2Stream.Core.Interfaces
{
    public interface IScoreboardService
    {
        #region Public Properties

        IEnumerable<ClipType> ClipTypes { get; }

        string ClockGame { get; }

        bool ClockGameIsUpToDate { get; }

        bool ClockNotFromClip { get; set; }

        string ClockShot { get; }

        bool ClockShotIsUpToDate { get; }

        Color ColorGuest { get; set; }

        bool ColorGuestUpToDate { get; }

        Color ColorHome { get; set; }

        bool ColorHomeUpToDate { get; }

        bool IsGameOver { get; set; }

        bool IsGameOverUpToDate { get; }

        string Message { get; }

        string Period { get; set; }

        bool PeriodNotFromClip { get; set; }

        string Periods { get; set; }

        bool PeriodsUpToDate { get; }

        bool PeriodUpToDate { get; }

        string ScoreGuest { get; set; }

        bool ScoreGuestUpToDate { get; }

        string ScoreHome { get; set; }

        bool ScoreHomeUpToDate { get; }

        bool ScoreNotFromClip { get; set; }

        bool ShotNotFromClip { get; set; }

        string TeamGuest { get; set; }

        bool TeamGuestUpToDate { get; }

        string TeamHome { get; set; }

        bool TeamHomeUpToDate { get; }

        (string, bool)[] Tickers { get; }

        int TickersFrequency { get; set; }

        bool[] TickersUpToDate { get; }

        bool UpToDate { get; }

        #endregion Public Properties

        #region Public Methods

        void RemoveClip(Clip clip);

        void SetClip(Clip clip, ClipType clipType);

        void SetTicker(int number, string text);

        void SetTicker(int number, bool isActive);

        void Update();

        #endregion Public Methods
    }
}