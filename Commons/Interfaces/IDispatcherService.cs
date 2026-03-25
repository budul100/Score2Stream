using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
{
    public interface IDispatcherService
    {
        #region Public Methods

        Task InvokeAsync(Action action, CancellationToken cancellationToken = default);

        Task InvokeAsync(IEnumerable<Action> actions, CancellationToken cancellationToken = default);

        Task<T> InvokeAsync<T>(Func<T> function, CancellationToken cancellationToken = default);

        void Post(Action action);

        #endregion Public Methods
    }
}