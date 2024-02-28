using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IVideoService
        : IDisposable
    {
        #region Public Properties

        IAreaService AreaService { get; }

        Bitmap Bitmap { get; }

        bool IsActive { get; }

        bool IsEnded { get; }

        string Name { get; }

        TimeSpan? ProcessingTime { get; }

        #endregion Public Properties

        #region Public Methods

        Task RunAsync(Input input);

        void Stop();

        #endregion Public Methods
    }
}