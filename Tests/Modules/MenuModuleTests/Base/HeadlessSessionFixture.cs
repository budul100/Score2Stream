using System;
using Avalonia.Headless;

namespace Score2Stream.Tests.MenuModuleTests.Base
{
    public class HeadlessSessionFixture : IDisposable
    {
        #region Public Constructors

        public HeadlessSessionFixture()
        {
            Session = HeadlessUnitTestSession.StartNew(typeof(TestApp.App));
        }

        #endregion Public Constructors

        #region Public Properties

        public HeadlessUnitTestSession Session { get; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Session?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods
    }
}