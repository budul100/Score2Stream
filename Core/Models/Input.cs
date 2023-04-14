using Score2Stream.Core.Interfaces;

namespace Score2Stream.Core.Models
{
    public class Input
    {
        #region Public Constructors

        public Input(bool isFile)
        {
            IsFile = isFile;
        }

        #endregion Public Constructors

        #region Public Properties

        public IClipService ClipService => VideoService?.ClipService;

        public int? DeviceId { get; set; }

        public string FileName { get; set; }

        public bool IsActive
        {
            get { return VideoService?.IsActive ?? false; }
            set { }
        }

        public bool IsFile { get; }

        public string Name { get; set; }

        public ISampleService SampleService => TemplateService?.SampleService;

        public ITemplateService TemplateService => ClipService?.TemplateService;

        public IVideoService VideoService { get; set; }

        #endregion Public Properties
    }
}