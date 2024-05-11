using OpenCvSharp;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Models.Contents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.VideoService.Extensions
{
    internal static class SegmentExtensions
    {
        #region Public Methods

        public static IEnumerable<Match> GetMatches(this Segment segment,
            bool preventMultipleComparison, double thresholdMatching)
        {
            var relevants = segment?.Area?.Template?.Samples?.ToArray();

            if (segment.Mat != default
                && relevants?.Any() == true)
            {
                foreach (var relevant in relevants)
                {
                    var similarity = relevant.Mat.GetSimilarityTo(
                        template: segment.Mat,
                        preventMultipleComparison: preventMultipleComparison);

                    var type = similarity >= thresholdMatching && similarity < Constants.SimilarityMax
                        ? MatchType.Similar
                        : MatchType.None;

                    var result = new Match
                    {
                        Sample = relevant,
                        Type = type,
                        Similarity = similarity,
                    };

                    yield return result;
                }
            }
        }

        public static void SetValue(this Segment segment, string value, int similarity,
            TimeSpan waitingDuration)
        {
            if (segment.ValueCurrent != value)
            {
                segment.ValueCurrent = value;
                segment.SimilarityCurrent = similarity;

                segment.TimeCurrent = DateTime.Now;
            }
            else if (segment.Value != segment.ValueCurrent
                && DateTime.Now > segment.TimeCurrent.Add(waitingDuration))
            {
                segment.Value = segment.ValueCurrent;
                segment.Similarity = segment.SimilarityCurrent;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static double GetSimilarityTo(this Mat image, Mat template, bool preventMultipleComparison)
        {
            var result = default(double);

            if (image.HasValue()
                && template.HasValue())
            {
                var compare = image.Resize(
                    dsize: template.Size(),
                    interpolation: InterpolationFlags.Nearest);

                var matchSqDiff = compare.MatchTemplate(
                    templ: template,
                    method: TemplateMatchModes.SqDiffNormed);

                matchSqDiff.MinMaxLoc(
                    minVal: out double minSqDiff,
                    maxVal: out double _);

                var absMinSqDiff = 1 - Math.Abs(minSqDiff);

                if (preventMultipleComparison)
                {
                    result = absMinSqDiff;
                }
                else
                {
                    var matchCCoeff = compare.MatchTemplate(
                        templ: template,
                        method: TemplateMatchModes.CCoeffNormed);

                    matchCCoeff.MinMaxLoc(
                        minVal: out double _,
                        maxVal: out double maxCCoeff);

                    var matchCCorr = compare.MatchTemplate(
                        templ: template,
                        method: TemplateMatchModes.CCorrNormed);

                    matchCCorr.MinMaxLoc(
                        minVal: out double _,
                        maxVal: out double maxCCorr);

                    var absMaxCCoeff = Math.Abs(maxCCoeff);
                    var absMaxCCorr = Math.Abs(maxCCorr);

                    result = absMinSqDiff * absMaxCCoeff * absMaxCCorr;

                    if (result == 0
                        && ((absMinSqDiff > Constants.SimilarityMin && absMinSqDiff < Constants.SimilarityMax)
                        || (absMaxCCoeff > Constants.SimilarityMin && absMaxCCoeff < Constants.SimilarityMax)
                        || (absMaxCCorr > Constants.SimilarityMin && absMaxCCorr < Constants.SimilarityMax)))
                    {
                        result = Constants.SimilarityStep;
                    }
                }
            }

            return result;
        }

        #endregion Private Methods
    }
}