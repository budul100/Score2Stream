using System;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace Score2Stream.RecognitionService
{
    public class DigitPrediction
    {
        #region Public Properties

        public float Confidence { get; init; }

        public int Digit { get; init; }

        #endregion Public Properties
    }

    public class DigitRecognizer(string modelPath)
        : IDisposable
    {
        #region Private Fields

        private const int H = 96;
        private const int W = 64;

        private readonly InferenceSession session = new(modelPath);

        private bool isDisposed;

        #endregion Private Fields

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public DigitPrediction Predict(Mat image)
        {
            float[] input = Preprocess(image);
            var tensor = new DenseTensor<float>(input, [1, 1, H, W]);
            var inputs = new[] { NamedOnnxValue.CreateFromTensor("image", tensor) };

            using var outputs = session.Run(inputs);
            var logits = outputs[0].AsEnumerable<float>().ToArray();

            // Softmax → Wahrscheinlichkeiten
            float max = logits.Max(); // numerische Stabilität
            float[] exps = logits.Select(x => MathF.Exp(x - max)).ToArray();
            float sum = exps.Sum();
            float[] probs = exps.Select(x => x / sum).ToArray();

            float confidence = probs.Max();
            int digit = Array.IndexOf(probs, confidence);

            return new DigitPrediction { Digit = digit, Confidence = confidence };
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    session.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static float[] Preprocess(Mat image)
        {
            // Graustufe
            Mat gray = new Mat();
            if (image.Channels() > 1)
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            else
                gray = image.Clone();

            // Auf Modellgröße skalieren
            Mat resized = new Mat();
            Cv2.Resize(gray, resized, new Size(W, H));

            // Pixel als float-Array, normalisiert auf [-1, 1]
            float[] result = new float[H * W];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float pixel = resized.At<byte>(y, x) / 255f;
                    result[y * W + x] = (pixel - 0.5f) / 0.5f;
                }

            return result;
        }

        #endregion Private Methods
    }
}