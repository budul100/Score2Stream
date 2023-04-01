using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Core.Interfaces
{
    public interface IWebcamService
    {
        #region Public Properties

        int CameraDeviceId { get; set; }

        BitmapSource Content { get; }

        bool CropContents { get; set; }

        bool IsActive { get; }

        int ThresholdCompare { get; set; }

        #endregion Public Properties

        #region Public Methods

        Task StartAsync();

        Task StopAsync();

        #endregion Public Methods
    }
}