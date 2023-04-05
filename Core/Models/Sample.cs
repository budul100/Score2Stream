using OpenCvSharp;
using System.Windows.Media.Imaging;

namespace Core.Models
{
    public class Sample
    {
        #region Public Properties

        public BitmapSource Bitmap { get; set; }

        public Mat Image { get; set; }

        public double Similarity { get; set; }

        public Template Template { get; set; }

        public string Value { get; set; }

        #endregion Public Properties
    }
}