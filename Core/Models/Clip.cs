using OpenCvSharp;
using System.Windows.Media.Imaging;

namespace ScoreboardOCR.Core.Models
{
    public class Clip
    {
        #region Private Fields

        private const int ThresholdMonochromeDefault = 80;

        #endregion Private Fields

        #region Public Properties

        public BitmapSource Compare { get; set; }

        public BitmapSource Content { get; set; }

        public bool HasDimensions { get; set; }

        public Mat Image { get; set; }

        public string Name { get; set; }

        public double RelativeX1 { get; set; }

        public double RelativeX2 { get; set; }

        public double RelativeY1 { get; set; }

        public double RelativeY2 { get; set; }
        public int ThresholdMonochrome { get; set; } = ThresholdMonochromeDefault;

        #endregion Public Properties
    }
}