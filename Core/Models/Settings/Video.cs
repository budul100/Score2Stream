﻿namespace Score2Stream.Core.Models.Settings
{
    public class Video
    {
        #region Public Properties

        public string FilePathVideo { get; set; }

        public int ImagesQueueSize { get; set; } = Constants.ImageQueueSizeDefault;

        public bool NoCropping { get; set; }

        public int ProcessingDelay { get; set; } = Constants.DelayProcessingDefault;

        #endregion Public Properties
    }
}