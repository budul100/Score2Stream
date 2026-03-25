using System;
using Avalonia.Media.Imaging;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoService.Extensions
{
    internal static class SegmentExtensions
    {
        #region Public Methods

        public static void SetValue(this Segment segment, bool hasValue, string value,
            double similarity, TimeSpan waitingDuration)
        {
            if (segment.ValueCurrent != value)
            {
                segment.ValueCurrent = value;
                segment.HasValueCurrent = hasValue;

                segment.SimilarityCurrent = Convert.ToInt32(similarity * Constants.ThresholdDivider);
                segment.TimeCurrent = DateTime.Now;
            }
            else if (segment.Value != segment.ValueCurrent
                && DateTime.Now > segment.TimeCurrent.Add(waitingDuration))
            {
                segment.Value = segment.ValueCurrent;
                segment.HasValue = segment.HasValueCurrent;

                segment.Similarity = segment.SimilarityCurrent;
            }
        }

        #endregion Public Methods
    }
}