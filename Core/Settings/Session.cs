using Score2Stream.Core.Models;
using System.Collections.Generic;

namespace Score2Stream.Core.Settings
{
    public class Session
    {
        #region Private Fields

        private const int ImagesQueueSizeDefault = 3;
        private const int ProcessingDelayDefault = 100;

        #endregion Private Fields

        #region Public Properties

        public string FilePathVideo { get; set; }

        public int ImagesQueueSize { get; set; } = ImagesQueueSizeDefault;

        public List<Input> Inputs { get; set; } = new List<Input>();

        public bool NoCentering { get; set; }

        public int ProcessingDelay { get; set; } = ProcessingDelayDefault;

        #endregion Public Properties
    }
}