using System;
using System.Threading;
using System.Threading.Tasks;
using EventAggregatorMocker;
using Moq;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Events.Segment;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.MenuModule.ViewModels;

namespace Score2Stream.Tests.MenuModuleTests.Base
{
    public abstract class TestBase
    {
        #region Private Fields

        private readonly HeadlessSessionFixture fixture;

        #endregion Private Fields

        #region Protected Constructors

        protected TestBase(HeadlessSessionFixture fixture)
        {
            this.fixture = fixture;
        }

        #endregion Protected Constructors

        #region Protected Methods

        protected static Mock<IEventAggregator> CreateEventAggregatorMock()
        {
            var mock = new Mock<IEventAggregator>();

            // Area
            mock.RegisterNewMockedEvent<AreasChangedEvent>();
            mock.RegisterNewMockedEvent<AreasOrderedEvent>();
            mock.RegisterNewMockedEvent<AreaModifiedEvent, Area>();
            mock.RegisterNewMockedEvent<AreaSelectedEvent, Area>();

            // Graphics
            mock.RegisterNewMockedEvent<ServerStartedEvent>();
            mock.RegisterNewMockedEvent<ServerStoppedEvent>();

            // Input
            mock.RegisterNewMockedEvent<InputCenteringEvent>();
            mock.RegisterNewMockedEvent<InputEndedEvent, Input>();
            mock.RegisterNewMockedEvent<InputSelectedEvent, Input>();
            mock.RegisterNewMockedEvent<InputStartedEvent, Input>();
            mock.RegisterNewMockedEvent<InputUpdatedEvent>();

            // Menu
            mock.RegisterNewMockedEvent<DetectionChangedEvent>();
            mock.RegisterNewMockedEvent<FilterChangedEvent>();
            mock.RegisterNewMockedEvent<TabSelectedEvent, ViewType>();

            // Sample
            mock.RegisterNewMockedEvent<SampleModifiedEvent, Sample>();
            mock.RegisterNewMockedEvent<SampleSelectedEvent, Sample>();
            mock.RegisterNewMockedEvent<SampleUpdatedEvent, Sample>();
            mock.RegisterNewMockedEvent<SamplesChangedEvent>();
            mock.RegisterNewMockedEvent<SamplesOrderedEvent>();

            // Scoreboard
            mock.RegisterNewMockedEvent<ScoreboardModifiedEvent>();
            mock.RegisterNewMockedEvent<ScoreboardUpdatedEvent, string>();

            // Segment
            mock.RegisterNewMockedEvent<SegmentDrawnEvent, Segment>();
            mock.RegisterNewMockedEvent<SegmentModifiedEvent, Segment>();
            mock.RegisterNewMockedEvent<SegmentSelectedEvent, Segment>();
            mock.RegisterNewMockedEvent<SegmentUpdatedEvent, Segment>();

            // Template
            mock.RegisterNewMockedEvent<TemplateSelectedEvent, Template>();
            mock.RegisterNewMockedEvent<TemplatesChangedEvent>();

            EnableCallBaseOnAllEvents(mock);
            return mock;
        }

        /// <summary>
        /// Central factory for MenuViewModel — avoids duplication across test classes.
        /// </summary>
        protected static (MenuViewModel ViewModel, Mock<IInputService> InputServiceMock,
            Mock<ITemplateService> TemplateServiceMock, Mock<ISettingsService<Session>> SettingsServiceMock,
            Mock<IDialogService> DialogServiceMock, Mock<IWebService> WebServiceMock,
            Mock<IScoreboardService> ScoreboardServiceMock)
            CreateViewModel(Mock<IInputService> inputServiceMock = null,
            Mock<ITemplateService> templateServiceMock = null,
            Mock<ISettingsService<Session>> settingsServiceMock = null, Mock<IDialogService> dialogServiceMock = null,
            Mock<IEventAggregator> eventAggregatorMock = null, Mock<IWebService> webServiceMock = null,
            Mock<IScoreboardService> scoreboardServiceMock = null)
        {
            inputServiceMock ??= new Mock<IInputService>();
            templateServiceMock ??= new Mock<ITemplateService>();
            dialogServiceMock ??= new Mock<IDialogService>();
            eventAggregatorMock ??= CreateEventAggregatorMock();
            webServiceMock ??= new Mock<IWebService>();
            scoreboardServiceMock ??= new Mock<IScoreboardService>();

            if (settingsServiceMock == null)
            {
                settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(new Session());
            }

            var viewModel = new MenuViewModel(
                settingsService: settingsServiceMock.Object,
                webService: webServiceMock.Object,
                scoreboardService: scoreboardServiceMock.Object,
                inputService: inputServiceMock.Object,
                templateService: templateServiceMock.Object,
                regionManager: new Mock<IRegionManager>().Object,
                dialogService: dialogServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);

            return (viewModel, inputServiceMock, templateServiceMock,
                    settingsServiceMock, dialogServiceMock, webServiceMock,
                    scoreboardServiceMock);
        }

        protected async Task RunInSessionAsync(Func<Task> action)
        {
            await fixture.Session.Dispatch(async () => await action(), CancellationToken.None);
        }

        protected async Task RunInSessionAsync(Action action)
        {
            await fixture.Session.Dispatch(action, CancellationToken.None);
        }

        #endregion Protected Methods

        #region Private Methods

        private static void EnableCallBase<TEvent>(Mock<IEventAggregator> mock)
            where TEvent : EventBase, new()
        {
            var eventInstance = mock.Object.GetEvent<TEvent>();
            Mock.Get(eventInstance).CallBase = true;
        }

        private static void EnableCallBaseOnAllEvents(Mock<IEventAggregator> mock)
        {
            // Area
            EnableCallBase<AreasChangedEvent>(mock);
            EnableCallBase<AreasOrderedEvent>(mock);
            EnableCallBase<AreaModifiedEvent>(mock);
            EnableCallBase<AreaSelectedEvent>(mock);

            // Graphics
            EnableCallBase<ServerStartedEvent>(mock);
            EnableCallBase<ServerStoppedEvent>(mock);

            // Input
            EnableCallBase<InputCenteringEvent>(mock);
            EnableCallBase<InputEndedEvent>(mock);
            EnableCallBase<InputSelectedEvent>(mock);
            EnableCallBase<InputStartedEvent>(mock);
            EnableCallBase<InputUpdatedEvent>(mock);

            // Menu
            EnableCallBase<DetectionChangedEvent>(mock);
            EnableCallBase<FilterChangedEvent>(mock);
            EnableCallBase<TabSelectedEvent>(mock);

            // Sample
            EnableCallBase<SampleModifiedEvent>(mock);
            EnableCallBase<SampleSelectedEvent>(mock);
            EnableCallBase<SampleUpdatedEvent>(mock);
            EnableCallBase<SamplesChangedEvent>(mock);
            EnableCallBase<SamplesOrderedEvent>(mock);

            // Scoreboard
            EnableCallBase<ScoreboardModifiedEvent>(mock);
            EnableCallBase<ScoreboardUpdatedEvent>(mock);

            // Segment
            EnableCallBase<SegmentDrawnEvent>(mock);
            EnableCallBase<SegmentModifiedEvent>(mock);
            EnableCallBase<SegmentSelectedEvent>(mock);
            EnableCallBase<SegmentUpdatedEvent>(mock);

            // Template
            EnableCallBase<TemplateSelectedEvent>(mock);
            EnableCallBase<TemplatesChangedEvent>(mock);
        }

        #endregion Private Methods
    }
}