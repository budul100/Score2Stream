using OpenCvSharp;
using ScoreboardOCR.Core.Models;

namespace WebcamService.Models
{
    internal struct RecClip
    {
        #region Public Properties

        public Clip Clip { get; set; }

        public Rect Rect { get; set; }

        #endregion Public Properties
    }
}