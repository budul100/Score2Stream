using System;
using System.IO;
using EventAggregatorMocker;
using Moq;
using OpenCvSharp;
using Prism.Events;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Events.Training;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Models.Contents;
using Xunit;

namespace Score2Stream.Tests.RecognitionServiceTests
{
    public class Tests
    {
        #region Private Fields

        private const string SamplesPath = @"..\..\..\..\..\Samples\Images";

        #endregion Private Fields

        #region Public Methods

        [Fact]
        public void RecognizeNumbers()
        {
            var eventAggregatorMock = CreateEventAggregatorMock();

            var recognitionService = new RecognitionService.Service(eventAggregatorMock.Object);

            var path0 = Path.Combine(SamplesPath, "SevenSegment-0.png");
            var bytes0 = GetBytes(path0);
            var result0 = recognitionService.Recognize(bytes0).Value;

            Assert.Equal(
                "0",
                result0);

            var samplesPath = Path.GetFullPath(SamplesPath);

            var path3 = Path.Combine(samplesPath, "SevenSegment-3.png");
            var bytes3 = GetBytes(path3);
            var result3 = recognitionService.Recognize(bytes3).Value;

            Assert.Equal(
                "3",
                result3);

            var path4 = Path.Combine(samplesPath, "SevenSegment-4.png");
            var bytes4 = GetBytes(path4);
            var result4 = recognitionService.Recognize(bytes4).Value;

            Assert.Equal(
                "4",
                result4);

            var path5 = Path.Combine(samplesPath, "SevenSegment-5.png");
            var bytes5 = GetBytes(path5);
            var result5 = recognitionService.Recognize(bytes5).Value;

            Assert.Equal(
                "5",
                result5);
        }

        #endregion Public Methods

        #region Private Methods

        private static Mock<IEventAggregator> CreateEventAggregatorMock()
        {
            var mock = new Mock<IEventAggregator>();

            mock.RegisterNewMockedEvent<AreaModifiedEvent, Area>();
            mock.RegisterNewMockedEvent<AreasChangedEvent>();
            mock.RegisterNewMockedEvent<AreaSelectedEvent, Area>();
            mock.RegisterNewMockedEvent<InputCenteringEvent>();
            mock.RegisterNewMockedEvent<DetectionChangedEvent>();
            mock.RegisterNewMockedEvent<FilterChangedEvent>();
            mock.RegisterNewMockedEvent<InputsChangedEvent>();
            mock.RegisterNewMockedEvent<SamplesChangedEvent>();
            mock.RegisterNewMockedEvent<SamplesOrderedEvent>();
            mock.RegisterNewMockedEvent<SampleSelectedEvent, Sample>();
            mock.RegisterNewMockedEvent<ScoreboardModifiedEvent>();
            mock.RegisterNewMockedEvent<SegmentSelectedEvent, Segment>();
            mock.RegisterNewMockedEvent<SegmentUpdatedEvent, Segment>();
            mock.RegisterNewMockedEvent<ServerStartedEvent>();
            mock.RegisterNewMockedEvent<TabSelectedEvent, ViewType>();
            mock.RegisterNewMockedEvent<TemplatesChangedEvent>();
            mock.RegisterNewMockedEvent<TemplateSelectedEvent, Template>();
            mock.RegisterNewMockedEvent<InputEndedEvent>();
            mock.RegisterNewMockedEvent<InputStartedEvent>();
            mock.RegisterNewMockedEvent<InputUpdatedEvent>();
            mock.RegisterNewMockedEvent<TrainingChangedEvent>();

            return mock;
        }

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

            var monochromeFrame = frame.AsMonochrome(0.6);

            var noiselessFrame = monochromeFrame.WithoutNoise(
                erodeIterations: 2,
                dilateIterations: 2);

            var centeredFrame = noiselessFrame.AsCentered(
                fullWidth: noiselessFrame.Width + 10,
                fullHeight: noiselessFrame.Height + 10);

            var result = centeredFrame.Clone();

            return result;
        }

        #endregion Private Methods
    }
}