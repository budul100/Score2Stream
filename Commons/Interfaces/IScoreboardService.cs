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

        bool ClockNotDetected { get; set; }

        string ClockShot { get; }

        bool ClockShotIsUpToDate { get; }

        Color ColorGuest { get; set; }

        bool ColorGuestUpToDate { get; }

        Color ColorHome { get; set; }

        bool ColorHomeUpToDate { get; }

        string FoulsGuest { get; set; }

        bool FoulsGuestUpToDate { get; }

        string FoulsHome { get; set; }

        bool FoulsHomeUpToDate { get; }

        bool FoulsNotDetected { get; set; }

        bool IsGameOver { get; set; }

        bool IsGameOverUpToDate { get; }

        bool IsUpToDate { get; }

        string Message { get; }

        string Period { get; set; }

        bool PeriodNotDetected { get; set; }

        string Periods { get; set; }

        bool PeriodsUpToDate { get; }

        bool PeriodUpToDate { get; }

        string ScoreGuest { get; set; }

        bool ScoreGuestUpToDate { get; }

        string ScoreHome { get; set; }

        bool ScoreHomeUpToDate { get; }

        bool ScoreNotDetected { get; set; }

        bool ShotNotDetected { get; set; }

        bool ShowTenthOfSecs { get; set; }

        string TeamGuest { get; set; }

        bool TeamGuestUpToDate { get; }

        string TeamHome { get; set; }

        bool TeamHomeUpToDate { get; }

        (string, bool)[] Tickers { get; }

        int TickersFrequency { get; set; }

        bool[] TickersUpToDate { get; }

        #endregion Public Properties

        #region Public Methods

        void Bind(Area area, AreaType type);

        void ReleaseArea(Area area);

        void SetTicker(int number, string text);

        void SetTicker(int number, bool isActive);

        void Update();

        #endregion Public Methods
    }
}