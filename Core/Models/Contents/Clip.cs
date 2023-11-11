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
        : Nameable
    {
        #region Private Fields

        private const int NoiseRemovalDefault = 0;
        private const int ThresholdMonochromeDefault = 50;

        #endregion Private Fields

        #region Public Properties

        [JsonIgnore]
        public Bitmap Bitmap { get; set; }

        [JsonIgnore]
        public string Description => Type != ClipType.None
            ? Type.GetDescription()
            : Name;

        public bool HasDimensions { get; set; }

        public int Height { get; set; }

        [JsonIgnore]
        public Queue<Mat> Images { get; set; } = new Queue<Mat>();

        public int Index { get; set; }

        [JsonIgnore]
        public Mat Mat { get; set; }

        public int NoiseRemoval { get; set; } = NoiseRemovalDefault;

        [JsonIgnore]
        public Rect? Rect { get; set; }

        [JsonIgnore]
        public int Similarity { get; set; }

        [JsonIgnore]
        public Template Template { get; set; }

        public string TemplateName { get; set; }

        public int ThresholdMonochrome { get; set; } = ThresholdMonochromeDefault;

        [JsonIgnore]
        public DateTime TimeDetection { get; set; }

        [JsonIgnore]
        public DateTime TimeUpdate { get; set; }

        public ClipType Type { get; set; } = ClipType.None;

        [JsonIgnore]
        public int UpdateSimilarity { get; set; }

        [JsonIgnore]
        public string UpdateValue { get; set; }

        [JsonIgnore]
        public string Value { get; set; }

        [JsonIgnore]
        public string ValueLast { get; set; }

        public int Width { get; set; }

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