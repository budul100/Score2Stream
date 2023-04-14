using Score2Stream.Core.Models;
using System.Collections.Generic;

namespace Score2Stream.Core.Interfaces
{
    public interface IInputService
    {
        #region Public Properties

        IClipService ClipService { get; }

        IList<Input> Inputs { get; }

        bool IsActive { get; }

        ISampleService SampleService { get; }

        ITemplateService TemplateService { get; }

        IVideoService VideoService { get; }

        #endregion Public Properties

        #region Public Methods

        void Select(int deviceId);

        void Select(string fileName);

        void Update();

        #endregion Public Methods
    }
}