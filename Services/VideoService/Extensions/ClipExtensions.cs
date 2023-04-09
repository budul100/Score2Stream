using Core.Models;
using System;
using System.Linq;

namespace VideoService.Extensions
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

        public static void SetValue(this Clip clip, string value, TimeSpan waitingSpan)
        {
            if (clip.UpdateValue != value)
            {
                clip.UpdateTime = DateTime.Now;
                clip.UpdateValue = value;
            }

            if (clip.Value != clip.UpdateValue
                && clip.UpdateTime.Add(waitingSpan) < DateTime.Now)
            {
                clip.Value = clip.UpdateValue;
            }
        }

        #endregion Public Methods
    }
}