using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.VideoService.Extensions
{
    internal static class SegmentExtensions
    {
        #region Public Methods

        public static IEnumerable<Match> GetMatches(this Segment segment, double thresholdMatching)
        {
            if (segment?.Mat == null || segment.Mat == default || segment.Mat.IsEmpty())
                yield break;

            var relevants = segment.Area?.Template?.Samples?
                .Where(s => s.Mat.HasValue() && !s.Mat.IsEmpty()).ToArray();

            if (relevants?.Length > 0)
            {
                foreach (var relevant in relevants)
                {
                    var similarity = relevant.Mat.GetSimilarityTo(segment.Mat);

                    var type = similarity >= thresholdMatching
                        ? MatchType.Similar
                        : MatchType.None;

                    yield return new Match
                    {
                        Sample = relevant,
                        Type = type,
                        Similarity = similarity,
                    };
                }
            }
        }

        public static void SetValue(this Segment segment, bool hasValue, string value,
            float? similarity, TimeSpan waitingDuration)
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

        #region Private Methods

        private static double GetSimilarityTo(this Mat image, Mat template)
        {
            if (!image.HasValue() || !template.HasValue())
                return default;

            var compare = image.Resize(
                dsize: template.Size(),
                interpolation: InterpolationFlags.Nearest);

            var matchCCoeff = compare.MatchTemplate(
                templ: template,
                method: TemplateMatchModes.CCoeffNormed);

            matchCCoeff.MinMaxLoc(
                minVal: out double _,
                maxVal: out double maxCCoeff);

            return Math.Abs(maxCCoeff);
        }

        #endregion Private Methods
    }
}