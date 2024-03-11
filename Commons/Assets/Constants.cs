namespace Score2Stream.Commons.Assets
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

        public const int ExitCodeStandard = 0;

        public const char GameClockSplitterDefault = ':';

        public const int ImageQueueSizeMax = 20;
        public const int ImageQueueSizeMin = 1;

        public const string LockFileName = ".lock";

        public const int MaxCountAreas = 20;
        public const int MaxCountInputs = 5;
        public const int MaxCountSamples = 100;
        public const int MaxCountTemplates = 10;

        public const int RecognitionDurationMax = 1000 * 60;

        public const float RotateLeftMax = -10F;
        public const float RotateRightMax = 10F;
        public const float RotateStep = 0.2F;

        public const int SegmentsCountMax = 3;
        public const int SegmentsCountMin = 1;

        public const int SizeChangeMin = 10;

        public const string SplitterTitle = " | ";
        public const char SplitterVersion = '.';

        public const string TabBoard = "Board";
        public const string TabClips = "Clips";
        public const string TabSamples = "Samples";

        public const double ThresholdDivider = 100;
        public const int ThresholdMax = 100;

        public const int TickersSize = 6;

        public const double ZoomMax = 5;
        public const double ZoomMin = 0.5;

        #endregion Public Fields
    }
}