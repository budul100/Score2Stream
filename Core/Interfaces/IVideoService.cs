using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Core.Interfaces
{
    public interface IVideoService
        : IDisposable
    {
        #region Public Properties

        BitmapSource Bitmap { get; }

        IClipService ClipService { get; }

        bool CropImage { get; set; }

        int Delay { get; set; }

        bool IsActive { get; }

        string Name { get; }

        int ThresholdCompare { get; set; }

        #endregion Public Properties

        #region Public Methods

        Task RunAsync(int deviceId, string name, IClipService clipService);

        #endregion Public Methods
    }
}