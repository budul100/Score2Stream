using System.Collections.Generic;
using System.Linq;
using Hompus.VideoInputDevices;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.InputService.Helpers
{
    public class DeviceEnumerator
        : IInputEnumerator
    {
        #region Public Methods

        public IReadOnlyDictionary<int, string> GetDevices()
        {
            using var enumerator = new SystemDeviceEnumerator();

            return enumerator.ListVideoInputDevice()
                .ToDictionary(d => d.Key, d => d.Value);
        }

        #endregion Public Methods
    }
}