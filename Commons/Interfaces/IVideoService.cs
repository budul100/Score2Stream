using Avalonia.Media.Imaging;
using Score2Stream.Commons.Models.Contents;
using System;
using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
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

        bool NoCropping { get; set; }

        int ProcessingDelay { get; set; }

        TimeSpan? ProcessingTime { get; }

        float Rotation { get; set; }

        bool RotationLeftPossible { get; }

        bool RotationRightPossible { get; }

        int ThresholdMatching { get; set; }

        int WaitingDuration { get; set; }

        #endregion Public Properties

        #region Public Methods

        Task RunAsync(Input input);

        void Stop();

        #endregion Public Methods
    }
}