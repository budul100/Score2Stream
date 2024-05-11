using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class Detection
    {
        #region Public Properties

        public int DurationDetectionWait { get; set; } = Defaults.DetectionWaitDefault;

        public bool FilterVerifieds { get; set; }

        public int MaxCountUnverifieds { get; set; } = Defaults.DetectionUnverifiedsDefault;

        public bool NoMultiComparison { get; set; }

        public bool NoRecognition { get; set; }

        public int ThresholdDetecting { get; set; } = Defaults.DetectionThresholdDefault;

        public int ThresholdMatching { get; set; } = Defaults.DetectionMatchingDefault;

        #endregion Public Properties
    }
}