using System;
using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
{
    public interface IDispatcherService
    {
        #region Public Methods

        Task InvokeAsync(Action action);

        Task<T> InvokeAsync<T>(Func<T> function);

        void Post(Action action);

        #endregion Public Methods
    }
}