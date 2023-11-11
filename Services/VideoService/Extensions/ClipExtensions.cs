using Score2Stream.Core;
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

        public static bool IsNeighbour(this Clip clip, string value)
        {
            if (clip.Value?.Length == 1
                && value?.Length == 1
                && char.IsNumber(clip.Value[0])
                && char.IsNumber(value[0]))
            {
                var last = default(char);

                if (clip.ValueLast?.Length == 1)
                {
                    if (DateTime.Now > clip.TimeUpdate.AddMilliseconds(Constants.DurationKeepLast))
                    {
                        clip.ValueLast = default;
                    }
                    else if (char.IsNumber(clip.ValueLast[0]))
                    {
                        last = clip.ValueLast[0];
                    }
                }

                switch (clip.Value[0])
                {
                    case '0': return value[0] is '0' || value[0] is '1' || value[0] is '2' || value[0] is '5' || value[0] is '9';

                    case '1': return value[0] is '1' || ((last == default || last is '2') && value[0] is '0') || ((last == default || last is '0') && value[0] is '2');

                    case '2': return value[0] is '2' || ((last == default || last is '3') && value[0] is '1') || ((last == default || last is '1') && value[0] is '3');

                    case '3': return value[0] is '3' || ((last == default || last is '4') && value[0] is '2') || ((last == default || last is '2') && value[0] is '4');

                    case '4': return value[0] is '4' || ((last == default || last is '5') && value[0] is '3') || ((last == default || last is '3') && value[0] is '5');

                    case '5': return value[0] is '5' || ((last == default || last is '6') && value[0] is '4') || ((last == default || last is '4') && value[0] is '6');

                    case '6': return value[0] is '6' || ((last == default || last is '7') && value[0] is '5') || ((last == default || last is '5') && value[0] is '7');

                    case '7': return value[0] is '7' || ((last == default || last is '8') && value[0] is '6') || ((last == default || last is '6') && value[0] is '8');

                    case '8': return value[0] is '8' || ((last == default || last is '9') && value[0] is '7') || ((last == default || last is '7') && value[0] is '9');

                    case '9': return value[0] is '9' || ((last == default || last is '0') && value[0] is '8') || ((last == default || last is '8') && value[0] is '0');
                }
            }

            return false;
        }

        public static void SetValue(this Clip clip, string value, int similarity,
            TimeSpan waitingDuration)
        {
            if (clip.UpdateValue != value)
            {
                clip.TimeDetection = DateTime.Now;
                clip.UpdateValue = value;
                clip.UpdateSimilarity = similarity;
            }

            if (DateTime.Now > clip.TimeDetection.Add(waitingDuration))
            {
                if (clip.Value != clip.UpdateValue)
                {
                    clip.ValueLast = clip.Value;
                    clip.Value = clip.UpdateValue;

                    clip.TimeUpdate = DateTime.Now;
                }

                clip.Similarity = clip.UpdateSimilarity;
            }
        }

        #endregion Public Methods
    }
}