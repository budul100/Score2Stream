using Core.Models;
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

        int Delay { get; set; }

        bool IsActive { get; }

        string Name { get; }

        bool NoCentering { get; set; }

        int ThresholdMatching { get; set; }

        int ThresholdDetecting { get; set; }

        #endregion Public Properties

        #region Public Methods

        Task RunAsync(Input input);

        void StopAll();

        #endregion Public Methods
    }
}