using Score2Stream.Core.Interfaces;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        public IClipService ClipService => VideoService?.ClipService;

        public int? DeviceId { get; set; }

        public string FileName { get; set; }

        [JsonIgnore]
        public bool IsActive => VideoService?.IsActive ?? false;

        public bool IsFile { get; }

        public string Name { get; set; }

        [JsonIgnore]
        public ISampleService SampleService => TemplateService?.SampleService;

        [JsonIgnore]
        public ITemplateService TemplateService => ClipService?.TemplateService;

        [JsonIgnore]
        public IVideoService VideoService { get; set; }

        #endregion Public Properties
    }
}