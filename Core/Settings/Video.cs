namespace Score2Stream.Core.Settings
{
    public class Video
    {
        #region Private Fields

        private const int ImagesQueueSizeDefault = 3;
        private const int ProcessingDelayDefault = 100;

        #endregion Private Fields

        #region Public Properties

        public string FilePathVideo { get; set; }

        public int ImagesQueueSize { get; set; } = ImagesQueueSizeDefault;

        public bool NoCentering { get; set; }

        public int ProcessingDelay { get; set; } = ProcessingDelayDefault;

        #endregion Public Properties
    }
}