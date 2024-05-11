using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Extensions;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Score2Stream.Commons.Models.Contents
{
    public class Area
        : Nameable
    {
        #region Public Properties

        [JsonIgnore]
        public string Description => Type != AreaType.None
            ? Type.GetDescription()
            : Name;

        public bool HasDimensions { get; set; }

        public int Index { get; set; }

        public int NoiseRemoval { get; set; } = Defaults.AreaNoiseRemovalDefault;

        [JsonIgnore]
        public IEnumerable<Segment> Segments { get; set; }

        public int Size { get; set; }

        [JsonIgnore]
        public Template Template { get; set; }

        public string TemplateName { get; set; }

        public int ThresholdMonochrome { get; set; } = Defaults.AreaThresholdMonochromeDefault;

        public AreaType Type { get; set; } = AreaType.None;

        public double X1 { get; set; }

        [JsonIgnore]
        public double? X1Last { get; set; }

        public double X2 { get; set; }

        [JsonIgnore]
        public double? X2Last { get; set; }

        public double Y1 { get; set; }

        [JsonIgnore]
        public double? Y1Last { get; set; }

        public double Y2 { get; set; }

        [JsonIgnore]
        public double? Y2Last { get; set; }

        #endregion Public Properties
    }
}