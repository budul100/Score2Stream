using OpenCvSharp;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface IWebcamService
    {
        #region Public Properties

        BitmapSource Current { get; }

        double ThresholdCompare { get; set; }

        double ThresholdMonochrome { get; set; }

        #endregion Public Properties

        #region Public Methods

        string Get(Mat image, int firstX, int firstY, int secondX, int secondY);

        void Set(Mat image, int firstX, int firstY, int secondX, int secondY, string value);

        Task StartAsync();

        Task StopAsync();

        #endregion Public Methods
    }
}