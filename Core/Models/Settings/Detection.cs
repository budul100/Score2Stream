﻿namespace Score2Stream.Core.Models.Settings
{
    public class Detection
    {
        #region Private Fields

        private const int ThresholdDetectingDefault = 90;
        private const int ThresholdMatchingDefault = 40;
        private const int WaitingDurationDefault = 100;

        #endregion Private Fields

        #region Public Properties

        public bool NoRecognition { get; set; }

        public int ThresholdDetecting { get; set; } = ThresholdDetectingDefault;

        public int ThresholdMatching { get; set; } = ThresholdMatchingDefault;

        public int WaitingDuration { get; set; } = WaitingDurationDefault;

        #endregion Public Properties
    }
}