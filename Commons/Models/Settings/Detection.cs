using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class Detection
    {
        #region Public Properties

        public bool NoMultiComparison { get; set; }

        public bool NoRecognition { get; set; }

        public bool PreferNeighbors { get; set; }

        public int ThresholdDetecting { get; set; } = Defaults.DetectionThresholdDefault;

        public int ThresholdMatching { get; set; } = Defaults.DetectionMatchingDefault;

        public int UnverifiedsCount { get; set; } = Defaults.UnverifiedsCountDefault;

        public int WaitingDuration { get; set; } = Defaults.DetectionWaitingDefault;

        #endregion Public Properties
    }
}