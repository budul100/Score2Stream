using System;
using System.Threading;
using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
{
    public interface IDispatcherService
    {
        #region Public Methods

        Task InvokeAsync(Action action, CancellationToken cancellationToken = default);

        Task<T> InvokeAsync<T>(Func<T> function, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}