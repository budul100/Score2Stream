using System.Windows.Media.Imaging;

namespace ScoreboardOCR.Core.Models
{
    public class Clip
    {
        #region Public Properties

        public int BoxHeight { get; set; }

        public int BoxLeft { get; set; }

        public int BoxTop { get; set; }

        public int BoxWidth { get; set; }

        public BitmapSource Compare { get; set; }

        public BitmapSource Content { get; set; }

        public string Name { get; set; }

        public double RelativeX1 { get; set; }

        public double RelativeX2 { get; set; }

        public double RelativeY1 { get; set; }

        public double RelativeY2 { get; set; }

        public double ThresholdMonochrome { get; set; }

        #endregion Public Properties
    }
}