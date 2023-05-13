namespace Score2Stream.Core.Settings
{
    public class UserSettings
    {
        #region Private Fields

        private const int ImagesQueueSizeDefault = 3;
        private const int ProcessingDelayDefault = 100;
        private const int ThresholdDetectingDefault = 90;
        private const int ThresholdMatchingDefault = 40;
        private const int WaitingDurationDefault = 100;

        #endregion Private Fields

        #region Public Properties

        public string FilePathVideo { get; set; }

        public int ImagesQueueSize { get; set; } = ImagesQueueSizeDefault;

        public bool NoCentering { get; set; }

        public int ProcessingDelay { get; set; } = ProcessingDelayDefault;

        public int ThresholdDetecting { get; set; } = ThresholdDetectingDefault;

        public int ThresholdMatching { get; set; } = ThresholdMatchingDefault;

        public int WaitingDuration { get; set; } = WaitingDurationDefault;

        #endregion Public Properties
    }
}