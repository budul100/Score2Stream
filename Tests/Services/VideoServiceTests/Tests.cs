using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.VideoService;
using Xunit;

namespace Score2Stream.Tests.VideoServiceTests
{
    public class Tests
        : IDisposable
    {
        #region Private Fields

        private readonly Mock<AreaModifiedEvent> areaModifiedEventMock;
        private readonly Mock<IAreaService> areaServiceMock;
        private readonly Mock<IDispatcherService> dispatcherServiceMock;
        private readonly Mock<IEventAggregator> eventAggregatorMock;
        private readonly Mock<InputEndedEvent> inputEndedEventMock;
        private readonly Mock<InputStartedEvent> inputStartedEventMock;
        private readonly Mock<InputUpdatedEvent> inputUpdatedEventMock;
        private readonly Mock<ILogger<Service>> loggerMock;
        private readonly Mock<IRecognitionService> recognitionServiceMock;
        private readonly Mock<SegmentDrawnEvent> segmentDrawnEventMock;
        private readonly Mock<SegmentUpdatedEvent> segmentUpdatedEventMock;
        private readonly Mock<ISettingsService<Session>> settingsServiceMock;
        private readonly Service sut;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            areaServiceMock = new Mock<IAreaService>();
            dispatcherServiceMock = new Mock<IDispatcherService>();
            eventAggregatorMock = new Mock<IEventAggregator>();
            loggerMock = new Mock<ILogger<Service>>();
            recognitionServiceMock = new Mock<IRecognitionService>();
            settingsServiceMock = new Mock<ISettingsService<Session>>();

            inputEndedEventMock = new Mock<InputEndedEvent>();
            inputStartedEventMock = new Mock<InputStartedEvent>();
            inputUpdatedEventMock = new Mock<InputUpdatedEvent>();
            segmentDrawnEventMock = new Mock<SegmentDrawnEvent>();
            segmentUpdatedEventMock = new Mock<SegmentUpdatedEvent>();
            areaModifiedEventMock = new Mock<AreaModifiedEvent>();

            eventAggregatorMock
                .Setup(e => e.GetEvent<InputStartedEvent>())
                .Returns(inputStartedEventMock.Object);
            eventAggregatorMock
                .Setup(e => e.GetEvent<InputEndedEvent>())
                .Returns(inputEndedEventMock.Object);
            eventAggregatorMock
                .Setup(e => e.GetEvent<InputUpdatedEvent>())
                .Returns(inputUpdatedEventMock.Object);
            eventAggregatorMock
                .Setup(e => e.GetEvent<SegmentDrawnEvent>())
                .Returns(segmentDrawnEventMock.Object);
            eventAggregatorMock
                .Setup(e => e.GetEvent<SegmentUpdatedEvent>())
                .Returns(segmentUpdatedEventMock.Object);
            eventAggregatorMock
                .Setup(e => e.GetEvent<AreaModifiedEvent>())
                .Returns(areaModifiedEventMock.Object);

            var session = new Session
            {
                Video = new Video
                {
                    ProcessingDelay = 0,
                    Rotation = 0,
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

            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(
                    It.IsAny<Action>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Action, CancellationToken>((action, _) => action())
                .Returns(Task.CompletedTask);

            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(
                    It.IsAny<Func<object>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Func<object> f, CancellationToken _) => f());

            sut = new Service(
                settingsService: settingsServiceMock.Object,
                areaService: areaServiceMock.Object,
                dispatcherService: dispatcherServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object,
                recognitionService: recognitionServiceMock.Object,
                logger: loggerMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            ((IDisposable)sut).Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Dispose_AfterStop_DoesNotThrow()
        {
            sut.Stop();

            var exception = Record.Exception(() => ((IDisposable)sut).Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            ((IDisposable)sut).Dispose();

            var exception = Record.Exception(() => ((IDisposable)sut).Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WhenNeverStarted_DoesNotThrow()
        {
            var exception = Record.Exception(() => ((IDisposable)sut).Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public async Task Dispose_WhileRunAsyncIsRunning_DoesNotDeadlock()
        {
            var input = new Input(true)
            {
                DeviceId = 999,
                Name = "TestDevice",
                Guid = Guid.NewGuid()
            };

            var runTask = sut.RunAsync(input);

            await Task.Delay(100);

            var disposeTask = Task.Run(() => ((IDisposable)sut).Dispose());

            var completed = await Task.WhenAny(
                disposeTask,
                Task.Delay(TimeSpan.FromSeconds(5)));

            Assert.Equal(disposeTask, completed);
        }

        [Fact]
        public async Task RunAsync_SetsNameFromInput()
        {
            var input = new Input(false)
            {
                FileName = "nonexistent_file_xyz.mp4",
                Name = "ExpectedName",
                Guid = Guid.NewGuid()
            };

            await sut.RunAsync(input);

            Assert.Equal("ExpectedName", sut.Name);
        }

        [Fact]
        public async Task RunAsync_WhileAlreadyRunning_SecondCallIsIgnored()
        {
            var input = new Input(true)
            {
                DeviceId = 999,
                Name = "TestDevice",
                Guid = Guid.NewGuid()
            };

            var firstRun = sut.RunAsync(input);

            await Task.Delay(100);

            var secondRun = sut.RunAsync(input);

            sut.Stop();

            await Task.WhenAll(firstRun, secondRun);

            inputStartedEventMock.Verify(
                e => e.Publish(),
                Times.AtMostOnce);
        }

        [Fact]
        public async Task RunAsync_WithNonExistentFile_InputEndedEventPublished()
        {
            var input = new Input(false)
            {
                FileName = "nonexistent_file_xyz.mp4",
                Name = "TestFile",
                Guid = Guid.NewGuid()
            };

            await sut.RunAsync(input);

            inputEndedEventMock.Verify(
                e => e.Publish(),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task RunAsync_WithNonExistentFile_IsActiveRemainingFalse()
        {
            var input = new Input(false)
            {
                FileName = "nonexistent_file_xyz.mp4",
                Name = "TestFile",
                Guid = Guid.NewGuid()
            };

            await sut.RunAsync(input);

            Assert.False(sut.IsActive);
        }

        [Fact]
        public async Task RunAsync_WithNonExistentFile_IsEndedBecomesTrue()
        {
            var input = new Input(false)
            {
                FileName = "nonexistent_file_xyz.mp4",
                Name = "TestFile",
                Guid = Guid.NewGuid()
            };

            await sut.RunAsync(input);

            Assert.True(sut.IsEnded);
        }

        [Fact]
        public async Task RunAsync_WithNonExistentFile_LogsError()
        {
            var input = new Input(false)
            {
                FileName = "nonexistent_file_xyz.mp4",
                Name = "TestFile",
                Guid = Guid.NewGuid()
            };

            await sut.RunAsync(input);

            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Stop_WhenCalledMultipleTimes_DoesNotThrow()
        {
            sut.Stop();

            var exception = Record.Exception(() => sut.Stop());

            Assert.Null(exception);
        }

        [Fact]
        public void Stop_WhenNeverStarted_DoesNotThrow()
        {
            var exception = Record.Exception(() => sut.Stop());

            Assert.Null(exception);
        }

        #endregion Public Methods
    }
}