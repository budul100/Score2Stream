using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.Commons.Models.Contents
{
    public class Input
    {
        #region Public Constructors

        public Input(bool isDevice)
        {
            IsDevice = isDevice;
        }

        #endregion Public Constructors

        #region Public Properties

        public List<Area> Areas { get; set; }

        [JsonIgnore]
        public IAreaService AreaService => VideoService?.AreaService;

        public int? DeviceId { get; set; }

        public string FileName { get; set; }

        [JsonIgnore]
        public Guid Guid { get; set; }

        [JsonIgnore]
        public bool IsActive => VideoService?.IsActive ?? false;

        public bool IsDevice { get; }

        [JsonIgnore]
        public bool IsEnded => VideoService?.IsEnded ?? false;

        public string Name { get; set; }

        public List<Template> Templates { get; set; }

        [JsonIgnore]
        public ITemplateService TemplateService => AreaService?.TemplateService;

        [JsonIgnore]
        public IVideoService VideoService { get; set; }

        #endregion Public Properties
    }
}