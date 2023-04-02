using Core.Interfaces;

namespace Core.Models
{
    public class Input
    {
        #region Public Properties

        public IClipService ClipService => VideoService?.ClipService;

        public int DeviceId { get; set; }

        public bool IsActive
        {
            get { return VideoService?.IsActive ?? false; }
            set { }
        }

        public string Name { get; set; }

        public IVideoService VideoService { get; set; }

        #endregion Public Properties
    }
}