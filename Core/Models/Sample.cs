using Avalonia.Media.Imaging;
using OpenCvSharp;

namespace Score2Stream.Core.Models
{
    public class Sample
    {
        #region Public Properties

        public Bitmap Bitmap { get; set; }

        public Mat Image { get; set; }

        public double Similarity { get; set; }

        public Template Template { get; set; }

        public string Value { get; set; }

        #endregion Public Properties
    }
}