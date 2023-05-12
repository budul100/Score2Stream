using Score2Stream.Core.Models;
using System;
using System.Linq;

namespace Score2Stream.VideoService.Extensions
{
    internal static class ClipExtensions
    {
        #region Public Methods

        public static void SetSimilarities(this Clip clip)
        {
            if (clip?.Template?.Samples?.Any() == true)
            {
                foreach (var sample in clip.Template.Samples)
                {
                    sample.Similarity = sample.Image.GetSimilarityTo(clip.Image);
                }
            }
        }

        public static void SetValue(this Clip clip, string value, int similarity,
            TimeSpan waitingSpan)
        {
            if (clip.UpdateValue != value)
            {
                clip.UpdateTime = DateTime.Now;
                clip.UpdateValue = value;
                clip.UpdateSimilarity = similarity;
            }

            if (clip.Value != clip.UpdateValue
                && clip.UpdateTime.Add(waitingSpan) < DateTime.Now)
            {
                clip.Value = clip.UpdateValue;
                clip.Similarity = clip.UpdateSimilarity;
            }
        }

        #endregion Public Methods
    }
}