using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class Video
    {
        #region Public Properties

        public string FilePathVideo { get; set; }

        public int ImagesQueueSize { get; set; } = Constants.ImageQueueSizeDefault;

        public bool NoCropping { get; set; }

        public int ProcessingDelay { get; set; } = Constants.DelayProcessingDefault;

        public float Rotation { get; set; }

        #endregion Public Properties
    }
}