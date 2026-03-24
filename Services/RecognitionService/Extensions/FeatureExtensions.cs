using System;
using System.Linq;

namespace Score2Stream.RecognitionService.Extensions
{
    internal static class FeatureExtensions
    {
        #region Public Methods

        public static float CosineSimilarity(this float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return default;

            var dot = 0f;
            var normA = 0f;
            var normB = 0f;

            for (var index = 0; index < a.Length; index++)
            {
                dot += a[index] * b[index];
                normA += a[index] * a[index];
                normB += b[index] * b[index];
            }

            var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);

            var result = denom != 0
                ? dot / denom
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