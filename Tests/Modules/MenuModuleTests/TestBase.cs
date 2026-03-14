using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using EventAggregatorMocker;
using Moq;
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
using Score2Stream.Commons.Models.Contents;

public abstract class TestBase
{
    #region Protected Methods

    // Shared across all MenuModule tests
    protected static Mock<IEventAggregator> CreateEventAggregatorMock()
    {
        var mock = new Mock<IEventAggregator>();

        mock.RegisterNewMockedEvent<AreaModifiedEvent, Area>();
        mock.RegisterNewMockedEvent<AreasChangedEvent>();
        mock.RegisterNewMockedEvent<AreaSelectedEvent, Area>();
        mock.RegisterNewMockedEvent<DetectionChangedEvent>();
        mock.RegisterNewMockedEvent<FilterChangedEvent>();
        mock.RegisterNewMockedEvent<InputCenteringEvent>();
        mock.RegisterNewMockedEvent<InputEndedEvent, Input>();
        mock.RegisterNewMockedEvent<InputSelectedEvent, Input>();
        mock.RegisterNewMockedEvent<InputStartedEvent, Input>();
        mock.RegisterNewMockedEvent<InputUpdatedEvent>();
        mock.RegisterNewMockedEvent<SamplesChangedEvent>();
        mock.RegisterNewMockedEvent<SampleSelectedEvent, Sample>();
        mock.RegisterNewMockedEvent<SamplesOrderedEvent>();
        mock.RegisterNewMockedEvent<ScoreboardModifiedEvent>();
        mock.RegisterNewMockedEvent<ScoreboardUpdatedEvent, string>();
        mock.RegisterNewMockedEvent<SegmentSelectedEvent, Segment>();
        mock.RegisterNewMockedEvent<SegmentUpdatedEvent, Segment>();
        mock.RegisterNewMockedEvent<ServerStartedEvent>();
        mock.RegisterNewMockedEvent<TabSelectedEvent, ViewType>();
        mock.RegisterNewMockedEvent<TemplatesChangedEvent>();
        mock.RegisterNewMockedEvent<TemplateSelectedEvent, Template>();

        return mock;
    }

    protected static async Task RunInSessionAsync(Func<Task> action)
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(Score2Stream.Tests.TestApp.App));
        await session.Dispatch(async () => await action(), CancellationToken.None);
    }

    protected static async Task RunInSessionAsync(Action action)
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(Score2Stream.Tests.TestApp.App));
        await session.Dispatch(action, CancellationToken.None);
    }

    #endregion Protected Methods
}