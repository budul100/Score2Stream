using OpenCvSharp;
using System.Windows.Media.Imaging;

namespace ScoreboardOCR.Core.Models
{
    public class Sample
    {
        #region Public Properties

        public BitmapSource Content { get; set; }

        public Mat Image { get; set; }

        public string Value { get; set; }

        #endregion Public Properties
    }
}