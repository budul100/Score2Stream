using System.Collections.Generic;

namespace Score2Stream.Commons.Interfaces
{
    public interface IInputEnumerator
    {
        #region Public Methods

        IReadOnlyDictionary<int, string> GetDevices();

        #endregion Public Methods
    }
}