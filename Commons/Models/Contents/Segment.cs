using System;
using System.Collections.Generic;
using OpenCvSharp;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Models.Base;

namespace Score2Stream.Commons.Models.Contents
{
    public class Segment
        : Imageable
    {
        #region Public Properties

        public Area Area { get; set; }

        public bool HasValue { get; set; }

        public bool HasValueCurrent { get; set; }

        public Queue<Mat> Images { get; set; } = new Queue<Mat>();

        public IEnumerable<Match> Matches { get; set; }

        public int Position { get; set; }

        public Rect? Rect { get; set; }

        public int Similarity { get; set; }

        public int SimilarityCurrent { get; set; }

        public DateTime TimeCurrent { get; set; }

        public SegmentType Type { get; set; } = SegmentType.None;

        public string Value { get; set; }

        public string ValueCurrent { get; set; }

        public double X1 { get; set; }

        public double X2 { get; set; }

        public double Y1 => Area.Y1;

        public double Y2 => Area.Y2;

        #endregion Public Properties
    }
}