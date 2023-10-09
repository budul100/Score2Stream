using Score2Stream.Core.Extensions;
using Score2Stream.Core.Models.Contents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.VideoService.Extensions
{
    internal static class ClipExtensions
    {
        #region Public Methods

        public static IEnumerable<KeyValuePair<double, Sample>> GetMatches(this Clip clip)
        {
            var relevants = clip?.Template?.Samples?
                .Where(s => !string.IsNullOrWhiteSpace(s.Value)).ToArray();

            if (relevants?.Any() == true)
            {
                foreach (var relevant in relevants)
                {
                    var similarity = relevant.Mat.GetSimilarityTo(clip.Mat);

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
            if (clip.UpdateValue != value)
            {
                clip.UpdateTime = DateTime.Now;
                clip.UpdateValue = value;
                clip.UpdateSimilarity = similarity;
            }

            if (DateTime.Now > clip.UpdateTime.Add(waitingDuration))
            {
                if (clip.Value != clip.UpdateValue)
                {
                    clip.Value = clip.UpdateValue;
                }

                clip.Similarity = clip.UpdateSimilarity;
            }
        }

        #endregion Public Methods
    }
}