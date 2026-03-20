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
    public class Tests
    {
        #region Private Fields

        private const string SamplesPath = @"..\..\..\..\..\Samples\Images";

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
                    DurationDetectionWait = 0,
                }
            };

            settingsServiceMock
                .Setup(s => s.Contents)
                .Returns(session);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void RecognizeNumbers()
        {
            var recognitionService = new RecognitionService.Service(settingsServiceMock.Object);

            Assert.Equal("0", Recognize(recognitionService, "SevenSegment-0.png"));
            Assert.Equal("3", Recognize(recognitionService, "SevenSegment-3.png"));
            Assert.Equal("4", Recognize(recognitionService, "SevenSegment-4.png"));
            Assert.Equal("5", Recognize(recognitionService, "SevenSegment-5.png"));
        }

        #endregion Public Methods

        #region Private Methods

        private static Mat GetImage(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            using var video = new VideoCapture();
            video.Open(
                fileName: path,
                apiPreference: VideoCaptureAPIs.ANY);

            using var frame = new Mat();
            video.Read(frame);

            var monochromeFrame = frame.AsMonochrome(0.6);

            var noiselessFrame = monochromeFrame.WithoutNoise(
                erodeIterations: 2,
                dilateIterations: 2);

            var centeredFrame = noiselessFrame.AsCentered(
                fullWidth: noiselessFrame.Width + 10,
                fullHeight: noiselessFrame.Height + 10);

            return centeredFrame.Clone();
        }

        private static string Recognize(RecognitionService.Service recognitionService, string fileName)
        {
            var sample = new Sample
            {
                Image = GetImage(Path.Combine(Path.GetFullPath(SamplesPath), fileName))
            };

            recognitionService.Update(sample);

            return recognitionService.GetFromBase(sample)?.Value;
        }

        #endregion Private Methods
    }
}