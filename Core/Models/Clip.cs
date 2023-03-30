using System.Windows.Media.Imaging;

namespace ScoreboardOCR.Core.Models
{
    public class Clip
    {
        #region Public Properties

        public double? BoxHeight { get; set; }

        public double? BoxLeft { get; set; }

        public double? BoxTop { get; set; }

        public double? BoxWidth { get; set; }

        public BitmapSource Compare { get; set; }

        public BitmapSource Content { get; set; }

        public bool HasValue => BoxLeft.HasValue
            && BoxTop.HasValue;

        public string Name { get; set; }

        public double RelativeX1 { get; set; }

        public double RelativeX2 { get; set; }

        public double RelativeY1 { get; set; }

        public double RelativeY2 { get; set; }
        public double ThresholdMonochrome { get; set; }

        #endregion Public Properties
    }
}