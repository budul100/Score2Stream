using Avalonia.Media.Imaging;
using Score2Stream.Core.Models.Contents;
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

        int ImagesQueueSize { get; set; }

        bool IsActive { get; }

        bool IsEnded { get; }

        string Name { get; }

        bool NoCentering { get; set; }

        int ProcessingDelay { get; set; }

        TimeSpan? ProcessingTime { get; }

        int ThresholdMatching { get; set; }

        int WaitingDuration { get; set; }

        #endregion Public Properties

        #region Public Methods

        Task RunAsync(Input input);

        void Stop();

        #endregion Public Methods
    }
}