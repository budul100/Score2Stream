using Core.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace DispatcherService
{
    public class Service
        : IDispatcherService
    {
        #region Private Fields

        private readonly Dispatcher dispatcher;

        #endregion Private Fields

        #region Public Constructors

        public Service(Dispatcher dispatcher)
        {
            Debug.Assert(dispatcher != null);

            this.dispatcher = dispatcher;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsSynchronized => dispatcher.Thread == Thread.CurrentThread;

        #endregion Public Properties

        #region Public Methods

        public void BeginInvoke(Action action)
        {
            Debug.Assert(action != default);

            dispatcher.BeginInvoke(action);
        }

        public void Invoke(Action action)
        {
            Debug.Assert(action != default);

            dispatcher.Invoke(action);
        }

        #endregion Public Methods
    }
}