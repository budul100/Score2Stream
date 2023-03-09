using System;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface IDispatcherService
    {
        #region Public Properties

        bool IsSynchronized { get; }

        #endregion Public Properties

        #region Public Methods

        void BeginInvoke(Action action);

        void Invoke(Action action);

        #endregion Public Methods
    }
}