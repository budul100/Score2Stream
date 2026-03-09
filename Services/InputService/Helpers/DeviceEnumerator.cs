using System;
using System.Collections.Generic;
using Hompus.VideoInputDevices;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.InputService.Helpers
{
    public class DeviceEnumerator
        : IDeviceEnumerator
    {
        #region Private Fields

        private readonly SystemDeviceEnumerator enumerator = new();

        #endregion Private Fields

        #region Public Methods

        public void Dispose()
        {
            enumerator.Dispose();

            GC.SuppressFinalize(this);
        }

        public IReadOnlyDictionary<int, string> GetVideoDevices() => enumerator.ListVideoInputDevice();

        #endregion Public Methods
    }
}