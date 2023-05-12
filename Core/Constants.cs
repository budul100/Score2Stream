namespace Score2Stream.Core.Constants
{
    public static class Constants
    {
        #region Public Fields

        public const double DividerThreshold = 100;

        public const string InputFileText = "Select file ...";

        public const int PortHttpWebServer = 5003;
        public const int PortHttpWebSocket = 9000;

        public const int DefaultImagesQueueSize = 3;
        public const int DefaultProcessingDelay = 100;
        public const int DefaultThresholdDetecting = 90;
        public const int DefaultThresholdMatching = 40;
        public const int DefaultWaitingDuration = 100;

        public const int MinQueueSize = 1;
        public const int MaxQueueSize = 20;

        public const int MaxDuration = 1000;
        public const int MaxThreshold = 100;

        #endregion Public Fields
    }
}