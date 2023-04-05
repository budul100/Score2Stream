using System.Linq;
using VideoService.Models;

namespace VideoService.Extensions
{
    internal static class SampleExtensions
    {
        #region Public Methods

        public static void SetDifferences(this RecClip recClip)
        {
            if (recClip.Clip?.Template?.Samples?.Any() == true)
            {
                foreach (var sample in recClip.Clip.Template.Samples)
                {
                    sample.Similarity = sample.Image.GetSimilarityTo(recClip.Clip.Image);
                }
            }
        }

        #endregion Public Methods
    }
}