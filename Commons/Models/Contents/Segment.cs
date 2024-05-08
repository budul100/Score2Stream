using Avalonia.Media.Imaging;
using OpenCvSharp;
using Score2Stream.Commons.Enums;
using System;
using System.Collections.Generic;

namespace Score2Stream.Commons.Models.Contents
{
    public class Segment
    {
        #region Public Properties

        public Area Area { get; set; }

        public Bitmap Bitmap { get; set; }

        public Queue<Mat> Images { get; set; } = new Queue<Mat>();

        public Mat Mat { get; set; }

        public int Position { get; set; }

        public Rect? Rect { get; set; }

        public int Similarity { get; set; }

        public int SimilarityCurrent { get; set; }

        public DateTime TimeCurrent { get; set; }

        public DateTime TimeDetection { get; set; }

        public ClipType Type { get; set; } = ClipType.None;

        public string Value { get; set; }

        public string ValueCurrent { get; set; }

        public double X1 { get; set; }

        public double X2 { get; set; }

        public double Y1 => Area.Y1;

        public double Y2 => Area.Y2;

        #endregion Public Properties
    }
}