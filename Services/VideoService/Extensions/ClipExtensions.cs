using System;
using System.Collections.Generic;
using System.Linq;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoService.Extensions
{
    internal static class ClipExtensions
    {
        #region Public Methods

        public static IEnumerable<KeyValuePair<double, Sample>> GetMatches(this Clip clip,
            bool preventMultipleComparison)
        {
            var relevants = clip?.Area?.Template?.Samples?
                .Where(s => !string.IsNullOrWhiteSpace(s.Value)).ToArray();

            if (relevants?.Any() == true)
            {
                foreach (var relevant in relevants)
                {
                    var similarity = relevant.Mat.GetSimilarityTo(
                        template: clip.Mat,
                        preventMultipleComparison: preventMultipleComparison);

                    if (similarity != 1)
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
            if (clip.ValueCurrent != value)
            {
                clip.TimeDetection = DateTime.Now;
                clip.ValueCurrent = value;
                clip.SimilarityCurrent = similarity;
            }

            if (DateTime.Now > clip.TimeDetection.Add(waitingDuration))
            {
                if (clip.Value != clip.ValueCurrent)
                {
                    clip.Value = clip.ValueCurrent;

                    clip.TimeCurrent = DateTime.Now;
                }

                clip.Similarity = clip.SimilarityCurrent;
            }
        }

        #endregion Public Methods
    }
}