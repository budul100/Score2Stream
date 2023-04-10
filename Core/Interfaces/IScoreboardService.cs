using Core.Enums;
using Core.Models;

namespace Core.Interfaces
{
    public interface IScoreboardService
    {
        #region Public Properties

        bool IsGameOver { get; set; }

        string Message { get; }

        int Period { get; set; }

        int ScoreGuest { get; set; }

        int ScoreHome { get; set; }

        string TeamGuest { get; set; }

        string TeamHome { get; set; }

        string Ticker { get; set; }

        #endregion Public Properties

        #region Public Methods

        void SetClip(ClipType contentType, Clip clip);

        #endregion Public Methods
    }
}