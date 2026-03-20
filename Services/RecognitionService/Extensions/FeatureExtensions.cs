using System;
using System.Linq;

namespace Score2Stream.RecognitionService.Extensions
{
    internal static class FeatureExtensions
    {
        #region Public Methods

        public static float CosineSimilarity(this float[] a, float[] b)
        {
            var dot = a.Zip(b, (x, y) => x * y).Sum();
            var normA = MathF.Sqrt(a.Select(x => x * x).Sum());
            var normB = MathF.Sqrt(b.Select(x => x * x).Sum());

            var result = (normA * normB) != 0
                ? dot / (normA * normB)
                : default;

            return result;
        }

        public static float[] Softmax(this float[] logits)
        {
            var max = logits.Max();
            var exps = logits
                .Select(x => MathF.Exp(x - max)).ToArray();

            var sum = exps.Sum();

            var result = exps
                .Select(x => x / sum).ToArray();

            return result;
        }

        #endregion Public Methods
    }
}