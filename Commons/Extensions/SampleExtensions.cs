using System.Collections.Generic;
using System.Linq;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Extensions
{
    public static class SampleExtensions
    {
        #region Public Methods

        public static int GetIndex(this Sample sample)
        {
            var result = sample.IsVerified
                ? default
                : sample.Index;

            return result;
        }

        public static IList<Sample> GetUnfiltereds(this IEnumerable<Sample> samples)
        {
            var result = samples
                .Where(s => !s.IsFiltered).ToList();

            return result;
        }

        public static string GetValue(this Sample sample)
        {
            var result = sample.IsVerified
                ? sample.Value
                : default;

            return result;
        }

        #endregion Public Methods
    }
}