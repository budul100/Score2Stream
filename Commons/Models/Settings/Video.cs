using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class Video
    {
        #region Public Properties

        public string FilePathVideo { get; set; }

        public int ImagesQueueSize { get; set; } = Defaults.VideoImageQueueSize;

        public bool NoCropping { get; set; }

        public int DelayProcessing { get; set; } = Defaults.VideoDelayProcessing;

        #endregion Public Properties
    }
}