﻿using Score2Stream.Core.Models.Contents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.VideoService.Extensions
{
    internal static class ClipExtensions
    {
        #region Public Methods

        public static IEnumerable<KeyValuePair<double, Sample>> GetSimilarSamples(this Clip clip, double thresholdMatching)
        {
            var relevants = clip?.Template?.Samples?
                .Where(s => !string.IsNullOrWhiteSpace(s.Value)).ToArray();

            if (relevants?.Any() == true)
            {
                foreach (var relevant in relevants)
                {
                    var similarity = relevant.Image.GetSimilarityTo(clip.Image);

                    if (similarity > thresholdMatching)
                    {
                        var result = new KeyValuePair<double, Sample>(
                            key: similarity,
                            value: relevant);

                        yield return result;
                    }
                }
            }
        }

        public static void SetValue(this Clip clip, string value, int similarity,
            TimeSpan waitingDuration)
        {
            if (clip.UpdateValue != value)
            {
                clip.UpdateTime = DateTime.Now;
                clip.UpdateValue = value;
                clip.UpdateSimilarity = similarity;
            }

            if (clip.Value != clip.UpdateValue
                && clip.UpdateTime.Add(waitingDuration) < DateTime.Now)
            {
                clip.Value = clip.UpdateValue;
                clip.Similarity = clip.UpdateSimilarity;
            }
        }

        #endregion Public Methods
    }
}