using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;

namespace Score2Stream.RecognitionService
{
    public class Service
        : IRecognitionService, IDisposable
    {
        #region Private Fields

        private const string FileFeatures = "digit_features.onnx";
        private const string FileModel = "digit_model.onnx";
        private const string FolderData = "TrainedData";

        private const int SampleHeight = 96;
        private const int SampleWidth = 64;

        private readonly List<(float[] Features, Sample Sample)> sampleVectors = [];
        private readonly InferenceSession sessionFeature;
        private readonly InferenceSession sessionModel;
        private readonly ISettingsService<Session> settingsService;

        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService)
        {
            var modelPath = Path.Combine(
                path1: AppContext.BaseDirectory,
                path2: FolderData,
                path3: FileModel);

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException(
                    message: $"Trained data file '{modelPath}' not found.",
                    fileName: modelPath);
            }

            sessionModel = new InferenceSession(modelPath);

            var featurePath = Path.Combine(
                path1: AppContext.BaseDirectory,
                path2: FolderData,
                path3: FileFeatures);

            if (File.Exists(featurePath))
            {
                sessionFeature = new InferenceSession(featurePath);
            }

            this.settingsService = settingsService;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Add(Sample sample)
        {
            var features = ExtractFeatures(sample.Mat);

            sampleVectors.Add((features, sample));
        }

        public void Dispose()
        {
            Dispose(isDisposing: true);
            GC.SuppressFinalize(this);
        }

        public Match GetModelMatch(Mat image)
        {
            var result = default(Match);

            if (image.HasValue())
            {
                var preprocessed = GetPreprocessed(image);

                var (value, confidence) = RecognizeWithBaseModel(preprocessed);

                var thresholdMatching = Math.Abs(settingsService.Contents.Detection.ThresholdMatching)
                    / Constants.ThresholdDivider;

                if (confidence >= thresholdMatching)
                {
                    result = new Match
                    {
                        Value = value,
                        Sample = null,
                        Similarity = confidence,
                        Type = Commons.Enums.MatchType.Similar,
                    };
                }
            }

            return result;
        }

        public IEnumerable<Match> GetSampleMatches(Mat image)
        {
            if (image.HasValue() && sampleVectors.Count > 0)
            {
                var preprocessed = GetPreprocessed(image);
                var features = GetFeatures(preprocessed);

                var thresholdMatching = Math.Abs(settingsService.Contents.Detection.ThresholdMatching)
                    / Constants.ThresholdDivider;

                var relevants = sampleVectors
                    .Where(s => s.Sample.IsVerified)
                    .Select(s => (s.Sample, Similarity: CosineSimilarity(s.Features, features)))
                    .Where(s => s.Similarity >= thresholdMatching).ToArray();

                foreach (var relevant in relevants)
                {
                    var result = new Match
                    {
                        Value = relevant.Sample.Value,
                        Sample = relevant.Sample,
                        Similarity = relevant.Similarity,
                        Type = Commons.Enums.MatchType.Similar,
                    };

                    yield return result;
                }
            }
        }

        public void Remove(Sample sample)
        {
            var features = ExtractFeatures(sample.Mat);

            var closest = sampleVectors
                .OrderBy(s => CosineSimilarity(s.Features, features))
                .FirstOrDefault();

            if (closest != default)
            {
                sampleVectors.Remove(closest);
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    sessionModel.Dispose();
                    sessionFeature?.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static float CosineSimilarity(float[] a, float[] b)
        {
            var dot = a.Zip(b, (x, y) => x * y).Sum();
            var normA = MathF.Sqrt(a.Select(x => x * x).Sum());
            var normB = MathF.Sqrt(b.Select(x => x * x).Sum());

            var result = (normA * normB) != 0
                ? dot / (normA * normB)
                : default;

            return result;
        }

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

            var size = new Size(
                Width: SampleWidth,
                Height: SampleHeight);

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

        private static float[] Softmax(float[] logits)
        {
            var max = logits.Max();
            var exps = logits
                .Select(x => MathF.Exp(x - max)).ToArray();
            var sum = exps.Sum();

            var result = exps
                .Select(x => x / sum).ToArray();

            return result;
        }

        private float[] ExtractFeatures(Mat image)
        {
            var preprocessed = GetPreprocessed(image);

            var result = GetFeatures(preprocessed);

            return result;
        }

        private float[] GetFeatures(float[] preprocessed)
        {
            var tensor = new DenseTensor<float>(
                memory: preprocessed,
                dimensions: [1, 1, SampleHeight, SampleWidth]);

            var input = NamedOnnxValue.CreateFromTensor(
                name: "image",
                value: tensor);

            using var outputs = sessionFeature.Run([input]);

            var result = outputs[0]
                .AsEnumerable<float>().ToArray();

            return result;
        }

        private (string Value, float Confidence) RecognizeWithBaseModel(float[] preprocessed)
        {
            var tensor = new DenseTensor<float>(
                memory: preprocessed,
                dimensions: [1, 1, SampleHeight, SampleWidth]);

            var input = NamedOnnxValue.CreateFromTensor(
                name: "image",
                value: tensor);

            using var outputs = sessionModel.Run([input]);

            var logits = outputs[0]
                .AsEnumerable<float>().ToArray();

            var probs = Softmax(logits);

            var confidence = probs.Max();
            var predicted = Array.IndexOf(probs, confidence);

            var result = (predicted.ToString(), confidence);
            return result;
        }

        #endregion Private Methods
    }
}