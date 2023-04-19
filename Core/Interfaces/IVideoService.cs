using Avalonia.Media.Imaging;
using Score2Stream.Core.Models;
using System;
using System.Threading.Tasks;

namespace Score2Stream.Core.Interfaces
{
    public interface IVideoService
        : IDisposable
    {
        #region Public Properties

        Bitmap Bitmap { get; }

        IClipService ClipService { get; }

        int Delay { get; set; }

        bool IsActive { get; }

        string Name { get; }

        bool NoCentering { get; set; }

        TimeSpan? ProcessingTime { get; }

        int ThresholdDetecting { get; set; }

        int ThresholdMatching { get; set; }

        int WaitingDuration { get; set; }

        #endregion Public Properties

        #region Public Methods

        Task RunAsync(Input input);

        void StopAll();

        #endregion Public Methods
    }
}