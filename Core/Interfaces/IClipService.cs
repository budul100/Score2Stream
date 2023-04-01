using Core.Models.Receiver;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface IClipService
    {
        #region Public Properties

        Clip Clip { get; }

        List<Clip> Clips { get; }

        #endregion Public Properties

        #region Public Methods

        void Add();

        bool IsUniqueName(string name);

        void Remove();

        #endregion Public Methods
    }
}