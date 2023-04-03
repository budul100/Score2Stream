using Core.Models;
using OpenCvSharp;

namespace VideoService.Models
{
    internal struct RecClip
    {
        #region Public Properties

        public Clip Clip { get; set; }

        public Rect Rect { get; set; }

        #endregion Public Properties
    }
}