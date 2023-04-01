using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Core.Interfaces
{
    public interface IWebcamService
    {
        #region Public Properties

        BitmapSource Content { get; }

        bool CropContents { get; set; }

        bool IsActive { get; }

        double ThresholdCompare { get; set; }

        #endregion Public Properties

        #region Public Methods

        Task StartAsync();

        Task StopAsync();

        #endregion Public Methods
    }
}