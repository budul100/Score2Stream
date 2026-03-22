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

        [JsonIgnore]
        public int? DeviceId { get; set; }

        public string DeviceName { get; set; }

        public string FileName { get; set; }

        public bool IsActive { get; set; }

        public bool IsDevice { get; set; }

        [JsonIgnore]
        public bool IsStarted => VideoService?.IsStarted == true;

        [JsonIgnore]
        public string Name { get; set; }

        public int OffsetX { get; set; }

        public int OffsetY { get; set; }

        public float Rotation { get; set; }

        [JsonIgnore]
        public IVideoService VideoService { get; set; }

        #endregion Public Properties
    }
}