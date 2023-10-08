namespace Score2Stream.Core
{
    public static class Constants
    {
        #region Public Fields

        public const double DividerThreshold = 100;

        public const int DurationUpdateTitle = 500;

        public const int MaxDuration = 1000;
        public const int MaxQueueSize = 20;
        public const int MaxThreshold = 100;
        public const int MinQueueSize = 1;

        public const int PortHttpWebServer = 5003;
        public const int PortHttpWebSocket = 9000;

        public const int RecognitionMilliSecondsMax = 1000 * 60 * 10;

        public const string SettingsFileNameDefault = "userSettings.json";

        public const string SplitterTitle = " | ";
        public const char SplitterVersion = '.';

        #endregion Public Fields
    }
}