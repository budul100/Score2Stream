using System;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.RecognitionService
{
    public class Service
        : IRecognitionService, IDisposable
    {
        #region Private Fields

        private const int SampleHeight = 96;
        private const int SampleWidth = 64;

        private const string TrainedDataFile = "digit_model.onnx";
        private const string TrainedDataFolder = "TrainedData";

        private readonly InferenceSession session;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service()
        {
            var modelPath = Path.Combine(
                path1: AppContext.BaseDirectory,
                path2: TrainedDataFolder,
                path3: TrainedDataFile);

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException(
                    message: $"The trained data file '{modelPath}' was not found.",
                    fileName: modelPath);
            }

            session = new(modelPath);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public (string Value, float Confidence) Recognize(Mat image)
        {
            var preprocessed = GetPreprocessed(image);

            var tensor = new DenseTensor<float>(
                memory: preprocessed,
                dimensions: [1, 1, SampleHeight, SampleWidth]);

            var value = NamedOnnxValue.CreateFromTensor(
                name: "image",
                value: tensor);

            var inputs = new[] { value };

            using var outputs = session.Run(inputs);

            var logits = outputs[0]
                .AsEnumerable<float>().ToArray();

            var max = logits.Max();
            var exps = logits
                .Select(x => MathF.Exp(x - max)).ToArray();

            var sum = exps.Sum();
            var probs = exps
                .Select(x => x / sum).ToArray();

            var confidence = probs.Max();

            var predicted = Array.IndexOf(
                array: probs,
                value: confidence);

            var result = (predicted.ToString(), confidence);

            return result;
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

        private static float[] GetPreprocessed(Mat image)
        {
            var gray = new Mat();

            if (image.Channels() > 1)
            {
                Cv2.CvtColor(
                    src: image,
                    dst: gray,
                    code: ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                gray = image.Clone();
            }

            var resized = new Mat();
            var size = new Size(SampleWidth, SampleHeight);

            Cv2.Resize(
                src: gray,
                dst: resized,
                dsize: size);

            var result = new float[SampleHeight * SampleWidth];

            for (var y = 0; y < SampleHeight; y++)
            {
                for (var x = 0; x < SampleWidth; x++)
                {
                    var pixel = resized.At<byte>(y, x) / 255f;
                    result[y * SampleWidth + x] = (pixel - 0.5f) / 0.5f;
                }
            }

            return result;
        }

        #endregion Private Methods
    }
}