using Score2Stream.Core.Models;
using System.Collections.Generic;

namespace Score2Stream.Core.Interfaces
{
    public interface IInputService
    {
        #region Public Properties

        IClipService ClipService { get; }

        int ImagesQueueSize { get; set; }

        IList<Input> Inputs { get; }

        bool IsActive { get; }

        bool NoCentering { get; set; }

        int ProcessingDelay { get; set; }

        ISampleService SampleService { get; }

        ITemplateService TemplateService { get; }

        int ThresholdDetecting { get; set; }

        int ThresholdMatching { get; set; }

        IVideoService VideoService { get; }

        int WaitingDuration { get; set; }

        #endregion Public Properties

        #region Public Methods

        void Initialize();

        void Select(Input input);

        void Select(string fileName);

        void StopAll();

        void Update();

        #endregion Public Methods
    }
}