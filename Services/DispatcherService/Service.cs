using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.DispatcherService
{
    public class Service
        : IDispatcherService
    {
        #region Public Methods

        public async Task<T> InvokeAsync<T>(Func<T> callback, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(callback);

            var result = default(T);

            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    result = await Dispatcher.UIThread.InvokeAsync(
                        callback: callback,
                        priority: DispatcherPriority.Background,
                        cancellationToken: cancellationToken);
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                { }
            }

            return result;
        }

        public async Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Dispatcher.UIThread.InvokeAsync(
                        callback: action,
                        priority: DispatcherPriority.Background,
                        cancellationToken: cancellationToken);
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                { }
            }
        }

        #endregion Public Methods
    }
}