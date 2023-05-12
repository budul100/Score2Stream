using Avalonia.Media.Imaging;
using OpenCvSharp;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Extensions;
using System;
using System.Collections.Generic;

namespace Score2Stream.Core.Models
{
    public class Clip
    {
        #region Public Properties

        public Bitmap Bitmap { get; set; }

        public string Description => Type != ClipType.None
            ? Type.GetDescription()
            : Name;

        public bool HasDimensions { get; set; }

        public Mat Image { get; set; }

        public Queue<Mat> Images { get; set; } = new Queue<Mat>();

        public string Name { get; set; }

        public Rect? Rect { get; set; }

        public double RelativeX1 { get; set; }

        public double RelativeX2 { get; set; }

        public double RelativeY1 { get; set; }

        public double RelativeY2 { get; set; }

        public int Similarity { get; set; }

        public Template Template { get; set; }

        public int ThresholdMonochrome { get; set; }

        public ClipType Type { get; set; }

        public int UpdateSimilarity { get; set; }

        public DateTime UpdateTime { get; set; }

        public string UpdateValue { get; set; }

        public string Value { get; set; }

        #endregion Public Properties
    }
}