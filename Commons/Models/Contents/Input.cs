using System.Collections.Generic;
using System.Text.Json.Serialization;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.Commons.Models.Contents
{
    public class Input
    {
        #region Public Properties

        public List<Area> Areas { get; set; }

        [JsonIgnore]
        public IAreaService AreaService => VideoService?.AreaService;

        public int? DeviceId { get; set; }

        public string DeviceName { get; set; }

        public string FileName { get; set; }

        public bool IsActive => VideoService?.IsActive == true;

        public bool IsStarted => VideoService?.IsStarted == true;

        public bool IsDevice { get; set; }

        public string Name { get; set; }

        public float Rotation { get; set; }

        public List<Template> Templates { get; set; }

        [JsonIgnore]
        public ITemplateService TemplateService => AreaService?.TemplateService;

        [JsonIgnore]
        public IVideoService VideoService { get; set; }

        #endregion Public Properties
    }
}