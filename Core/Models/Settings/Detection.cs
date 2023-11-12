namespace Score2Stream.Core.Models.Settings
{
    public class Detection
    {
        #region Public Properties

        public bool NoRecognition { get; set; }

        public float Rotation { get; set; }

        public int ThresholdDetecting { get; set; } = Constants.DetectionThresholdDefault;

        public int ThresholdMatching { get; set; } = Constants.DetectionMatchingDefault;

        public int WaitingDuration { get; set; } = Constants.DetectionWaitingDefault;

        #endregion Public Properties
    }
}