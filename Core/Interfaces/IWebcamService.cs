using OpenCvSharp;
using ScoreboardOCR.Core.Models;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface IWebcamService
    {
        #region Public Properties

        BitmapSource Content { get; }

        double ThresholdCompare { get; set; }

        double ThresholdMonochrome { get; set; }

        #endregion Public Properties

        #region Public Methods

        void AddClip(Clip clip);

        string Get(Mat image, int firstX, int firstY, int secondX, int secondY);

        void RemoveClip(Clip clip);

        void Set(Mat image, int firstX, int firstY, int secondX, int secondY, string value);

        Task StartAsync();

        Task StopAsync();

        #endregion Public Methods
    }
}