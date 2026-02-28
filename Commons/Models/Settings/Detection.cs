using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class Detection
    {
        #region Public Properties

        public int DurationDetectionWait { get; set; } = Defaults.DetectionWait;

        public bool FilterVerifieds { get; set; }

        public int MaxCountUnverifieds { get; set; } = Defaults.DetectionUnverifieds;

        public int ThresholdDetecting { get; set; } = Defaults.DetectionThreshold;

        public int ThresholdMatching { get; set; } = Defaults.DetectionMatching;

        #endregion Public Properties
    }
}