using System;
using System.Collections.Generic;

namespace Score2Stream.Commons.Interfaces
{
    public interface IDeviceEnumerator
        : IDisposable
    {
        #region Public Methods

        IReadOnlyDictionary<int, string> GetVideoDevices();

        #endregion Public Methods
    }
}