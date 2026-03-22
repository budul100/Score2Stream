using System;
using System.IO;
using Moq;
using OpenCvSharp;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Xunit;

namespace Score2Stream.Tests.RecognitionServiceTests
{
    public class Tests : IDisposable
    {
        #region Private Fields

        private const string SamplesPath = @"..\..\..\..\..\Samples\Images";

        private readonly RecognitionService.Service recognitionService;
        private readonly Mock<ISettingsService<Session>> settingsServiceMock;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            settingsServiceMock = new Mock<ISettingsService<Session>>();

            var session = new Session
            {
                Video = new Video
                {
                    ProcessingDelay = 0,
                    ImagesQueueSize = 5,
                    NoCropping = false,
                    FilePathVideo = string.Empty
                },
                Detection = new Detection
                {
                    ThresholdMatching = 80,
                    ThresholdDetecting = 80,
                    DurationDetectionWait = 0,
                }
            };

            settingsServiceMock
                .Setup(s => s.Contents)
                .Returns(session);

            recognitionService = new RecognitionService.Service(settingsServiceMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void Bind_EmptyImage_SetsDefaultFeatures()
        {
            var sample = new Sample { Image = new Mat() };

            recognitionService.Bind(sample);

            Assert.Null(sample.Features);
        }

        [Fact]
        public void Bind_ValidImage_SetsNormalized()
        {
            var sample = new Sample
            {
                Image = GetImage(Path.Combine(Path.GetFullPath(SamplesPath), "SevenSegment-0.png"))
            };

            recognitionService.Bind(sample);

            Assert.NotNull(sample.Normalized);
            Assert.NotEmpty(sample.Normalized);
        }

        [Fact]
        public void Detect_EmptyImage_ReturnsNull()
        {
            var sample = new Sample { Image = new Mat() };

            recognitionService.Bind(sample);
            var result = recognitionService.Detect(sample);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("SevenSegment-0.png", "0")]
        [InlineData("SevenSegment-3.png", "3")]
        [InlineData("SevenSegment-4.png", "4")]
        [InlineData("SevenSegment-5.png", "5")]
        public void Detect_KnownDigitImages_ReturnsCorrectValue(string fileName, string expected)
        {
            var result = Recognize(fileName);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Detect_ValidImage_ReturnsMatchWithValue()
        {
            var sample = new Sample
            {
                Image = GetImage(Path.Combine(Path.GetFullPath(SamplesPath), "SevenSegment-0.png"))
            };

            recognitionService.Bind(sample);
            var match = recognitionService.Detect(sample);

            Assert.NotNull(match);
            Assert.False(string.IsNullOrEmpty(match.Value));
            Assert.True(match.Similarity > 0f);
        }

        public void Dispose()
        {
            recognitionService?.Dispose();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            var service = new RecognitionService.Service(settingsServiceMock.Object);
            service.Dispose();

            var ex = Record.Exception(() => service.Dispose());

            Assert.Null(ex);
        }

        #endregion Public Methods

        #region Private Methods

        private static Mat GetImage(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            // Use ImRead directly – VideoCapture is unnecessary for static images
            var frame = Cv2.ImRead(path, ImreadModes.Color);

            var monochromeFrame = frame.AsMonochrome(0.6);
            var noiselessFrame = monochromeFrame.WithoutNoise(
                erodeIterations: 2,
                dilateIterations: 2);
            var centeredFrame = noiselessFrame.AsCentered(
                fullWidth: noiselessFrame.Width + 10,
                fullHeight: noiselessFrame.Height + 10);

            return centeredFrame.Clone();
        }

        private string Recognize(string fileName)
        {
            var sample = new Sample
            {
                Image = GetImage(Path.Combine(Path.GetFullPath(SamplesPath), fileName))
            };

            recognitionService.Bind(sample);
            return recognitionService.Detect(sample)?.Value;
        }

        #endregion Private Methods
    }
}