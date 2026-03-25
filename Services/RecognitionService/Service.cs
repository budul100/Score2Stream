using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Base;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.RecognitionService.Extensions;

namespace Score2Stream.RecognitionService
{
    public class Service
        : IRecognitionService
    {
        #region Private Fields

        private const string FileFeatures = "digit_features.onnx";
        private const string FileModel = "digit_model.onnx";
        private const string FolderData = "TrainedData";

        private readonly InferenceSession sessionFeature;
        private readonly InferenceSession sessionModel;
        private readonly ISettingsService<Session> settingsService;

        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService)
        {
            this.settingsService = settingsService;

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

            if (!File.Exists(featurePath))
            {
                throw new FileNotFoundException(
                    message: $"Feature data file '{featurePath}' not found.",
                    fileName: featurePath);
            }

            sessionFeature = new InferenceSession(featurePath);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Bind(Imageable imageable)
        {
            imageable.Normalized = imageable.Image.GetNormalized(
                Constants.NormalizedHeight,
                Constants.NormalizedWidth);

            if (imageable.IsEmpty)
            {
                imageable.Features = default;
            }
            else
            {
                var tensor = new DenseTensor<float>(
                    memory: imageable.Normalized,
                    dimensions: [1, 1, Constants.NormalizedHeight, Constants.NormalizedWidth]);

                var input = NamedOnnxValue.CreateFromTensor(
                    name: "image",
                    value: tensor);

                using var outputs = sessionFeature.Run([input]);

                imageable.Features = outputs[0]
                    .AsEnumerable<float>().ToArray();
            }
        }

        public Match Detect(Imageable imageable)
        {
            var result = default(Match);

            if (!imageable.IsEmpty)
            {
                var (value, confidence) = GetValue(imageable.Normalized);

                var thresholdMatching = Math.Abs(settingsService.Contents.Detection.ThresholdMatching)
                    / Constants.ThresholdDivider;

                if (confidence >= thresholdMatching)
                {
                    result = new Match
                    {
                        Similarity = confidence,
                        Type = Commons.Enums.MatchType.Similar,
                        Value = value,
                    };
                }
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(isDisposing: true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<Match> GetMatches(Segment segment, IEnumerable<Sample> samples)
        {
            var relevants = samples?
                .Where(s => s?.Features != default).ToArray();

            if (segment?.Features != default
                && !segment.IsEmpty
                && relevants?.Length > 0)
            {
                var thresholdMatching = Math.Abs(settingsService.Contents.Detection.ThresholdMatching)
                    / Constants.ThresholdDivider;

                foreach (var relevant in relevants)
                {
                    var similarity = relevant.Features.CosineSimilarity(segment.Features);

                    var type = similarity >= thresholdMatching
                        ? Commons.Enums.MatchType.Similar
                        : Commons.Enums.MatchType.None;

                    var result = new Match
                    {
                        Similarity = similarity,
                        Type = type,
                        Value = relevant.Value,
                    };

                    yield return result;
                }
            }
        }

        public bool HasSimilars(Segment segment, IEnumerable<Sample> samples)
        {
            var result = false;

            var relevants = samples?
                .Where(s => s?.Features != default).ToArray();

            if (segment?.Features != default
                && !segment.IsEmpty
                && relevants?.Length > 0)
            {
                var thresholdDetecting = Math.Abs(settingsService.Contents.Detection.ThresholdDetecting)
                    / Constants.ThresholdDivider;

                result = relevants
                    .Select(s => s.Features.CosineSimilarity(segment.Features))
                    .Any(s => s >= thresholdDetecting);
            }

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
                    sessionModel.Dispose();
                    sessionFeature?.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private (string Value, float Confidence) GetValue(float[] preprocessed)
        {
            var tensor = new DenseTensor<float>(
                memory: preprocessed,
                dimensions: [1, 1, Constants.NormalizedHeight, Constants.NormalizedWidth]);

            var input = NamedOnnxValue.CreateFromTensor(
                name: "image",
                value: tensor);

            using var outputs = sessionModel.Run([input]);

            var logits = outputs[0]
                .AsEnumerable<float>().ToArray();

            var probs = logits.Softmax();

            var confidence = probs.Max();

            var predicted = Array.IndexOf(
                array: probs,
                value: confidence);

            var result = (predicted.ToString(), confidence);

            return result;
        }

        #endregion Private Methods
    }
}