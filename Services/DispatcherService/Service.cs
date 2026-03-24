using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Score2Stream.Commons.Assets;
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

            if (cancellationToken.IsCancellationRequested) return;

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

        public async Task InvokeAsync(IEnumerable<Action> actions, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actions);

            if (!(actions?.Count() > 0)
                || cancellationToken.IsCancellationRequested) return;

            try
            {
                foreach (var chunk in actions.Chunk(Constants.UpdateChunkSize))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await Dispatcher.UIThread.InvokeAsync(
                        callback: () => ProcessActions(chunk),
                        priority: DispatcherPriority.Background,
                        cancellationToken: cancellationToken);
                }
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            { }
        }

        #endregion Public Methods

        #region Private Methods

        private static void ProcessActions(IEnumerable<Action> actions)
        {
            foreach (var action in actions)
            {
                action.Invoke();
            }
        }

        #endregion Private Methods
    }
}