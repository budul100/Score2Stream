using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using Prism.Events;
using Score2Stream.Commons.Events.Training;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.RecognitionService
{
    public class Service
        : IRecognitionService, IDisposable
    {
        #region Private Fields

        private const int FeatureDim = 128;

        private const string FileFeatures = "digit_features.onnx";
        private const string FileModel = "digit_model.onnx";
        private const string FolderData = "TrainedData";

        private const float MinMeanBrightness = 0.05f;
        private const int SampleHeight = 96;
        private const int SampleWidth = 64;

        private static readonly (string Value, float Confidence) EmptyResult = (string.Empty, 1f);

        private readonly IEventAggregator eventAggregator;
        private readonly InferenceSession sessionFeature;
        private readonly InferenceSession sessionModel;
        private float[] headBias;
        private string[] headClasses;
        private float[][] headWeights;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

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
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsTrained => headWeights != null;

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(isDisposing: true);
            GC.SuppressFinalize(this);
        }

        public (string Value, float Confidence) Recognize(Mat image)
        {
            var preprocessed = GetPreprocessed(image);

            if (IsEmpty(image))
                return EmptyResult;

            var baseResult = RecognizeWithBaseModel(preprocessed);

            if (!IsTrained)
                return baseResult;

            var headResult = RecognizeWithHead(preprocessed);

            return headResult.Confidence > baseResult.Confidence
                ? headResult
                : baseResult;
        }

        public void Reset()
        {
            headWeights = null;
            headBias = null;
            headClasses = null;

            this.eventAggregator.GetEvent<TrainingChangedEvent>().Publish();
        }

        public void Train(IEnumerable<Sample> samples, int epochs = 50, float learningRate = 0.01f)
        {
            if (sessionFeature == null)
            {
                throw new InvalidOperationException(
                    $"Feature extractor '{FileFeatures}' not found. Cannot fine-tune.");
            }

            // --- 1. Collect labeled data

            var data = samples
                .Where(s => s.Mat != null && !string.IsNullOrEmpty(s.Value))
                .Select(s => (Features: ExtractFeatures(s.Mat), Label: s.Value)).ToList();

            if (data.Count == 0)
            {
                throw new ArgumentException(
                    "No valid samples provided for training.");
            }

            // --- 2. Build class index ---

            headClasses = data
                .Select(d => d.Label).Distinct()
                .OrderBy(v => v).ToArray();

            var classIndex = headClasses
                .Select((c, i) => (c, i))
                .ToDictionary(x => x.c, x => x.i);

            var numClasses = headClasses.Length;

            // --- 3. Initialize weights (Xavier) ---

            var rng = new Random(42);
            float scale = MathF.Sqrt(2f / FeatureDim);

            headWeights = Enumerable.Range(0, numClasses)
                .Select(_ => Enumerable.Range(0, FeatureDim)
                    .Select(__ => (float)(rng.NextDouble() * 2 - 1) * scale)
                    .ToArray()).ToArray();

            headBias = new float[numClasses];

            // --- 4. SGD training loop ---

            for (var epoch = 0; epoch < epochs; epoch++)
            {
                // Shuffle
                data = data
                    .OrderBy(_ => rng.Next()).ToList();

                var totalLoss = 0f;

                foreach (var (features, label) in data)
                {
                    var trueClass = classIndex[label];

                    // Forward: logits = W * x + b
                    var logits = ComputeLogits(features);

                    // Softmax
                    var probs = Softmax(logits);

                    // Cross-entropy loss
                    totalLoss -= MathF.Log(probs[trueClass] + 1e-9f);

                    // Backward: dL/dlogit = probs - one_hot(trueClass)
                    var dLogits = probs.ToArray();
                    dLogits[trueClass] -= 1f;

                    // Update weights
                    for (var c = 0; c < numClasses; c++)
                    {
                        for (var f = 0; f < FeatureDim; f++)
                        {
                            headWeights[c][f] -= learningRate * dLogits[c] * features[f];
                        }

                        headBias[c] -= learningRate * dLogits[c];
                    }
                }
            }

            this.eventAggregator.GetEvent<TrainingChangedEvent>().Publish();
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

        // Schwellwert, anpassbar
        private static bool IsEmpty(Mat image)
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

            var mean = Cv2.Mean(gray);

            return (mean.Val0 / 255f) < MinMeanBrightness;
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

        private float[] ComputeLogits(float[] features)
        {
            var numClasses = headWeights.Length;
            var logits = new float[numClasses];

            for (var c = 0; c < numClasses; c++)
            {
                var sum = headBias[c];

                for (int f = 0; f < FeatureDim; f++)
                    sum += headWeights[c][f] * features[f];

                logits[c] = sum;
            }

            return logits;
        }

        private float[] ExtractFeatures(Mat image)
        {
            var preprocessed = GetPreprocessed(image);

            var result = ExtractFeaturesFromPreprocessed(preprocessed);

            return result;
        }

        private float[] ExtractFeaturesFromPreprocessed(float[] preprocessed)
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

        private (string Value, float Confidence) RecognizeWithHead(float[] preprocessed)
        {
            var features = ExtractFeaturesFromPreprocessed(preprocessed);
            var logits = ComputeLogits(features);
            var probs = Softmax(logits);

            var confidence = probs.Max();
            var predicted = Array.IndexOf(
                array: probs,
                value: confidence);

            var result = (headClasses[predicted], confidence);
            return result;
        }

        #endregion Private Methods
    }
}