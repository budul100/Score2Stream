using Avalonia.Media.Imaging;
using OpenCvSharp;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Score2Stream.Core.Models.Contents
{
    public class Clip
    {
        #region Public Properties

        [JsonIgnore]
        public Bitmap Bitmap { get; set; }

        [JsonIgnore]
        public Mat Centred { get; set; }

        [JsonIgnore]
        public string Description => Type != ClipType.None
            ? Type.GetDescription()
            : Name;

        [JsonIgnore]
        public Mat Full { get; set; }

        public bool HasDimensions { get; set; }

        [JsonIgnore]
        public Queue<Mat> Images { get; set; } = new Queue<Mat>();

        public string Name { get; set; }

        [JsonIgnore]
        public Rect? Rect { get; set; }

        public double RelativeX1 { get; set; }

        public double RelativeX2 { get; set; }

        public double RelativeY1 { get; set; }

        public double RelativeY2 { get; set; }

        [JsonIgnore]
        public int Similarity { get; set; }

        public Template Template { get; set; }

        public int ThresholdMonochrome { get; set; }

        public ClipType Type { get; set; } = ClipType.None;

        [JsonIgnore]
        public int UpdateSimilarity { get; set; }

        [JsonIgnore]
        public DateTime UpdateTime { get; set; }

        [JsonIgnore]
        public string UpdateValue { get; set; }

        [JsonIgnore]
        public string Value { get; set; }

        #endregion Public Properties
    }
}