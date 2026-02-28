using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using EventAggregatorMocker;
using Moq;
using OpenCvSharp;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Assets;
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
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.MenuModule.ViewModels;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    public class SampleServiceTests
        : IDisposable
    {
        #region Private Fields

        private readonly Mat mat;

        #endregion Private Fields

        #region Public Constructors

        public SampleServiceTests()
        {
            mat = new Mat(new Size(10, 10), MatType.CV_16SC1);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            mat?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task SampleAdd_ExceedsMaxCount_DoesNotThrow()
        {
            using var session = HeadlessUnitTestSession.StartNew(typeof(Score2Stream.Tests.TestApp.App));

            await session.Dispatch(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                var totalAttempts = Constants.MaxCountSamples + 10;

                for (var i = 0; i < totalAttempts; i++)
                {
                    viewModel.SampleAddCommand.Execute();
                }

                Assert.True(sampleService.Samples.Count <= Constants.MaxCountSamples,
                    $"Sample count {sampleService.Samples.Count} should not exceed {Constants.MaxCountSamples}.");
            }, CancellationToken.None);
        }

        [Fact]
        public async Task SampleAdd_MultipleSamples_IncrementsCount()
        {
            using var session = HeadlessUnitTestSession.StartNew(typeof(Score2Stream.Tests.TestApp.App));

            await session.Dispatch(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                var expectedCount = 5;

                for (var i = 0; i < expectedCount; i++)
                {
                    viewModel.SampleAddCommand.Execute();
                }

                Assert.Equal(expectedCount, sampleService.Samples.Count);
            }, CancellationToken.None);
        }

        [Fact]
        public async Task SampleAdd_WithValidSegment_AddsSample()
        {
            using var session = HeadlessUnitTestSession.StartNew(typeof(Score2Stream.Tests.TestApp.App));

            await session.Dispatch(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.SampleAddCommand.Execute();

                Assert.Single(sampleService.Samples);
            }, CancellationToken.None);
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

        private (MenuViewModel ViewModel, ISampleService SampleService) CreateViewModelWithService()
        {
            var templateMock = new Mock<Template>();

            var area = new Area
            {
                Template = templateMock.Object,
            };

            var segment = new Segment
            {
                Area = area,
                Mat = mat,
            };

            var eventAggregatorMock = CreateEventAggregatorMock();

            var session = new Session
            {
                Detection = new Detection { PreventAutoRecognition = true }
            };

            var sessionSettingsServiceMock = new Mock<ISettingsService<Session>>();
            sessionSettingsServiceMock.Setup(m => m.Contents).Returns(session);

            var recognitionServiceMock = new Mock<IRecognitionService>();
            var dialogServiceMock = new Mock<IDialogService>();

            var sampleService = new SampleService.Service(
                settingsService: sessionSettingsServiceMock.Object,
                recognitionService: recognitionServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);

            sampleService.Initialize(templateMock.Object);

            var inputServiceMock = new Mock<IInputService>();
            inputServiceMock.Setup(m => m.SampleService).Returns(sampleService);
            inputServiceMock.Setup(m => m.AreaService.Segment).Returns(segment);

            var viewModel = new MenuViewModel(
                settingsService: sessionSettingsServiceMock.Object,
                webService: new Mock<IWebService>().Object,
                scoreboardService: new Mock<IScoreboardService>().Object,
                inputService: inputServiceMock.Object,
                regionManager: new Mock<IRegionManager>().Object,
                eventAggregator: eventAggregatorMock.Object);

            return (viewModel, sampleService);
        }

        #endregion Private Methods
    }
}