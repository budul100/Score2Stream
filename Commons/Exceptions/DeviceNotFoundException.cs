using System;

namespace Score2Stream.Commons.Exceptions
{
    public class DeviceNotFoundException
        : Exception
    {
        #region Public Constructors

        public DeviceNotFoundException()
            : base()
        { }

        public DeviceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public DeviceNotFoundException(string deviceName)
            : base($"The device '{deviceName}' was not found in the available devices.")
        { }

        #endregion Public Constructors
    }
}