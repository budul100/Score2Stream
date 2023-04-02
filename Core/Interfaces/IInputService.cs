using Core.Models;
using System.Collections.Generic;

namespace Core.Interfaces
{
    public interface IInputService
    {
        #region Public Properties

        IClipService ClipService { get; }

        IList<Input> Inputs { get; }

        bool IsActive { get; }

        IVideoService VideoService { get; }

        #endregion Public Properties

        #region Public Methods

        void Select(Input input);

        void Update();

        #endregion Public Methods
    }
}