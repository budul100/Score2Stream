using Avalonia.Media;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IScoreboardService
    {
        #region Public Properties

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

        bool ShowTenthOfSecs { get; set; }

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

        void BindArea(Area area, AreaType type);

        void ReleaseArea(Area area);

        void SetTicker(int number, string text);

        void SetTicker(int number, bool isActive);

        void Update();

        #endregion Public Methods
    }
}