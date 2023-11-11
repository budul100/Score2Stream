namespace Score2Stream.Core
{
    public static class Constants
    {
        #region Public Fields

        public const int ClipPositionFactor = 50;

        public const int DelayMin = 0;
        public const int DelayUpdate = 1;

        public const int DurationKeepLast = 500;
        public const int DurationMax = 1000;
        public const int DurationUpdateTitle = 500;

        public const int PortHttpWebServer = 5003;
        public const int PortHttpWebSocket = 9000;

        public const int QueueSizeMax = 20;
        public const int QueueSizeMin = 1;

        public const int RecognitionDurationMax = 1000 * 60;

        public const double SelectionFontSize = 16.0;
        public const double SelectionThicknessActive = 3.0;
        public const double SelectionThicknessNormal = 1.0;

        public const string SettingsFileNameDefault = "userSettings.json";

        public const string SplitterTitle = " | ";
        public const char SplitterVersion = '.';

        public const string TabBoard = "Board";
        public const string TabClips = "Clips";
        public const string TabSamples = "Samples";

        public const double ThresholdDivider = 100;
        public const int ThresholdMax = 100;

        public const double ZoomMax = 5;
        public const double ZoomMin = 0.5;

        #endregion Public Fields
    }
}