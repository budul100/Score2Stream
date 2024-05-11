using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using EventAggregatorMocker;
using Moq;
using OpenCvSharp;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Events.Video;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.MenuModule.ViewModels;
using Xunit;

namespace Tests.Modules.MenuModuleTests
{
    public class SampleServiceTests
    {
        #region Public Methods

        [Fact]
        public async Task SamplesAddAsync()
        {
            using var session = HeadlessUnitTestSession.StartNew(typeof(TestApp.App));

            await session.Dispatch(() =>
            {
                var viewModel = GetViewModel();

                var index = 0;

                while (index++ < Constants.MaxCountSamples + 10)
                {
                    viewModel.SampleAddCommand.Execute();
                }
            }, CancellationToken.None);
        }

        #endregion Public Methods

        #region Private Methods

        private static MenuViewModel GetViewModel()
        {
            var templateMock = new Mock<Template>();

            var area = new Area
            {
                Template = templateMock.Object,
            };

            var segment = new Segment
            {
                Area = area,
                Mat = new Mat(new OpenCvSharp.Size(10, 10), MatType.CV_16SC1),
            };

            var eventAggregatorMock = new Mock<IEventAggregator>();

            eventAggregatorMock.RegisterNewMockedEvent<AreaModifiedEvent, Area>();
            eventAggregatorMock.RegisterNewMockedEvent<AreasChangedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<AreaSelectedEvent, Area>();
            eventAggregatorMock.RegisterNewMockedEvent<FilterChangedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<InputsChangedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<SamplesChangedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<SampleSelectedEvent, Sample>();
            eventAggregatorMock.RegisterNewMockedEvent<ScoreboardModifiedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<SegmentSelectedEvent, Segment>();
            eventAggregatorMock.RegisterNewMockedEvent<SegmentUpdatedEvent, Segment>();
            eventAggregatorMock.RegisterNewMockedEvent<ServerStartedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<TemplatesChangedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<TemplateSelectedEvent, Template>();
            eventAggregatorMock.RegisterNewMockedEvent<VideoEndedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<VideoStartedEvent>();
            eventAggregatorMock.RegisterNewMockedEvent<VideoUpdatedEvent>();

            var session = new Session
            {
                Detection = new Detection { NoRecognition = true }
            };

            var sessionSettingsServiceMock = new Mock<ISettingsService<Session>>();

            sessionSettingsServiceMock.Setup(m => m.Contents).Returns(session);

            var webServiceMock = new Mock<IWebService>();
            var scoreboardServiceMock = new Mock<IScoreboardService>();
            var regionManagerMock = new Mock<IRegionManager>();

            var recognitionServiceMock = new Mock<IRecognitionService>();
            var dialogServiceMock = new Mock<IDialogService>();

            var sampleService = new Score2Stream.SampleService.Service(
                settingsService: sessionSettingsServiceMock.Object,
                recognitionService: recognitionServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);

            sampleService.Initialize(templateMock.Object);

            var inputServiceMock = new Mock<IInputService>();

            inputServiceMock.Setup(m => m.SampleService).Returns(sampleService);
            inputServiceMock.Setup(m => m.AreaService.Segment).Returns(segment);

            var result = new MenuViewModel(
                sessionSettingsServiceMock.Object,
                webServiceMock.Object,
                scoreboardServiceMock.Object,
                inputServiceMock.Object,
                regionManagerMock.Object,
                eventAggregatorMock.Object);

            return result;
        }

        #endregion Private Methods
    }
}