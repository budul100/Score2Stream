using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

public interface IVideoService
    : IDisposable, IAsyncDisposable
{
    #region Public Properties

    IAreaService AreaService { get; }

    Bitmap Bitmap { get; }

    bool IsActive { get; }

    bool IsStarted { get; }

    string Name { get; }

    TimeSpan? ProcessingTime { get; }

    #endregion Public Properties

    #region Public Methods

    Task RunAsync(Input input);

    Task StopAsync();

    #endregion Public Methods
}