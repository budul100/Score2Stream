using System;
using System.IO;
using OpenCvSharp;
using Score2Stream.Commons.Extensions;
using Xunit;

namespace Tests.Services.RecognitionServiceTests
{
    public class Tests
    {
        #region Private Fields

        private const string SamplesPath = @"..\..\..\Samples";

        #endregion Private Fields

        #region Public Methods

        [Fact]
        public void RecognizeNumbers()
        {
            var recognitionService = new Score2Stream.RecognitionService.Service();

            //var path0 = Path.Combine(SamplesPath, "SevenSegment-0.png");
            //var bytes0 = GetBytes(path0);
            //var result0 = recognitionService.Recognize(bytes0);

            //Assert.Equal(
            //    "0",
            //    result0);

            var samplesPath = Path.GetFullPath(SamplesPath);

            var path3 = Path.Combine(samplesPath, "SevenSegment-3.png");
            var bytes3 = GetBytes(path3);
            var result3 = recognitionService.Recognize(bytes3);

            Assert.Equal(
                "3",
                result3);

            var path4 = Path.Combine(samplesPath, "SevenSegment-4.png");
            var bytes4 = GetBytes(path4);
            var result4 = recognitionService.Recognize(bytes4);

            Assert.Equal(
                "4",
                result4);

            var path5 = Path.Combine(samplesPath, "SevenSegment-5.png");
            var bytes5 = GetBytes(path5);
            var result5 = recognitionService.Recognize(bytes5);

            //Assert.Equal(
            //    "5",
            //    result5);
        }

        #endregion Public Methods

        #region Private Methods

        private static Mat GetBytes(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception();
            }

            using var video = new VideoCapture();
            video.Open(
                fileName: path,
                apiPreference: VideoCaptureAPIs.ANY);

            using var frame = new Mat();
            video.Read(frame);

            var monochromeFrame = frame.ToMonochrome(0.6);

            var noiselessFrame = monochromeFrame.WithoutNoise(
                erodeIterations: 2,
                dilateIterations: 2);

            var centeredFrame = noiselessFrame.ToCentered(
                fullWidth: noiselessFrame.Width + 10,
                fullHeight: noiselessFrame.Height + 10);

            var result = centeredFrame.Clone();

            return result;
        }

        #endregion Private Methods
    }
}