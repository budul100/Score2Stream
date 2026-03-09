using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using OpenCvSharp;
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

        // Mock for our new IVideoCapture wrapper
        private readonly Mock<IVideoCapture> videoCaptureMock;

        private readonly Service videoService;
        private bool isDisposed;

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
            videoCaptureMock = new Mock<IVideoCapture>();

            inputEndedEventMock = new Mock<InputEndedEvent>();
            inputStartedEventMock = new Mock<InputStartedEvent>();
            inputUpdatedEventMock = new Mock<InputUpdatedEvent>();
            segmentDrawnEventMock = new Mock<SegmentDrawnEvent>();
            segmentUpdatedEventMock = new Mock<SegmentUpdatedEvent>();
            areaModifiedEventMock = new Mock<AreaModifiedEvent>();

            // Setup EventAggregator to return our mocked events
            eventAggregatorMock.Setup(e => e.GetEvent<InputStartedEvent>()).Returns(inputStartedEventMock.Object);
            eventAggregatorMock.Setup(e => e.GetEvent<InputEndedEvent>()).Returns(inputEndedEventMock.Object);
            eventAggregatorMock.Setup(e => e.GetEvent<InputUpdatedEvent>()).Returns(inputUpdatedEventMock.Object);
            eventAggregatorMock.Setup(e => e.GetEvent<SegmentDrawnEvent>()).Returns(segmentDrawnEventMock.Object);
            eventAggregatorMock.Setup(e => e.GetEvent<SegmentUpdatedEvent>()).Returns(segmentUpdatedEventMock.Object);
            eventAggregatorMock.Setup(e => e.GetEvent<AreaModifiedEvent>()).Returns(areaModifiedEventMock.Object);

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

            settingsServiceMock.Setup(s => s.Contents).Returns(session);

            // Mock DispatcherService to execute synchronously for tests to avoid thread issues
            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(It.IsAny<Action>(), It.IsAny<CancellationToken>()))
                .Callback<Action, CancellationToken>((action, _) => action())
                .Returns(Task.CompletedTask);

            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(It.IsAny<Func<object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Func<object> f, CancellationToken _) => f());

            // Initialize the Service using the Func<IVideoCapture> approach
            videoService = new Service(
                settingsService: settingsServiceMock.Object,
                areaService: areaServiceMock.Object,
                dispatcherService: dispatcherServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object,
                recognitionService: recognitionServiceMock.Object,
                videoCaptureFactory: () => videoCaptureMock.Object, // <-- Injecting the factory function here!
                logger: loggerMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void Constructor_InitialState_IsCorrect()
        {
            // Assert
            Assert.False(videoService.IsActive);
            Assert.False(videoService.IsEnded);
            Assert.Null(videoService.Name);
            Assert.Null(videoService.Bitmap);
            Assert.Null(videoService.ProcessingTime);
        }

        [Fact]
        public void Constructor_SetsAreaServiceProperty()
        {
            // Assert
            Assert.NotNull(videoService.AreaService);
            Assert.Equal(areaServiceMock.Object, videoService.AreaService);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Dispose_AfterStop_DoesNotThrow()
        {
            videoService.Stop();
            var exception = Record.Exception(() => ((IDisposable)videoService).Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            ((IDisposable)videoService).Dispose();
            var exception = Record.Exception(() => ((IDisposable)videoService).Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_WhenNeverStarted_DoesNotThrow()
        {
            var exception = Record.Exception(() => ((IDisposable)videoService).Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public async Task Dispose_WhileRunAsyncIsRunning_DoesNotDeadlock()
        {
            // Arrange
            var input = new Input
            {
                DeviceId = 999,
                DeviceName = "TestDevice",
                IsDevice = true,
                Name = "TestDevice",
            };

            // Make the capture block until cancellation happens
            videoCaptureMock.Setup(v => v.Read(It.IsAny<Mat>())).Returns(() =>
            {
                Task.Delay(500).Wait();
                return true;
            });

            // Act
            var runTask = videoService.RunAsync(input);
            await Task.Delay(50); // Wait briefly to let the thread start

            var disposeTask = Task.Run(() => ((IDisposable)videoService).Dispose());

            var completed = await Task.WhenAny(
                disposeTask,
                Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            Assert.Equal(disposeTask, completed); // Dispose must finish within 5 seconds
        }

        [Fact]
        public async Task RunAsync_SetsNameProperty()
        {
            // Arrange
            var input = new Input
            {
                FileName = "dummy.mp4",
                IsDevice = false,
                Name = "dummy",
            };

            // Act
            await videoService.RunAsync(input);

            // Assert
            // Check if Name gets assigned by the input.GetName() extension method during RunAsync
            Assert.NotNull(videoService.Name);
        }

        [Fact]
        public async Task RunAsync_WhileAlreadyRunning_SecondCallIsIgnored()
        {
            // Arrange
            var input = new Input
            {
                DeviceId = 999,
                DeviceName = "TestDevice",
                IsDevice = true,
                Name = "TestDevice",
            };

            // Act
            var firstRun = videoService.RunAsync(input);
            await Task.Delay(100);
            var secondRun = videoService.RunAsync(input);

            videoService.Stop();
            await Task.WhenAll(firstRun, secondRun);

            // Assert - Verify that the event is only published once, ensuring the second run didn't execute
            inputStartedEventMock.Verify(e => e.Publish(It.IsAny<Input>()), Times.AtMostOnce);
        }

        [Fact]
        public async Task RunAsync_WithNonExistentFile_LogsError()
        {
            // Arrange
            var input = new Input
            {
                FileName = "nonexistent_file_xyz.mp4",
                IsDevice = false,
                Name = "nonexistent_file_xyz",
            };

            // Setup the mock to return false, simulating a failed file opening
            videoCaptureMock.Setup(v => v.Open(It.IsAny<string>())).Returns(false);

            // Act
            await videoService.RunAsync(input);

            // Assert - Safe method to verify ILogger in Moq
            loggerMock.Verify(
                x => x.Log(
                    It.Is<Microsoft.Extensions.Logging.LogLevel>(l => l == Microsoft.Extensions.Logging.LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task RunAsync_WithValidInput_ProcessesFramesAndPublishesEvents()
        {
            // Arrange
            // Create a temporary dummy file on disk
            // so that System.IO.File.Exists(fileName) returns 'true' in the service
            var tempFileName = System.IO.Path.GetTempFileName();

            try
            {
                var input = new Input
                {
                    FileName = tempFileName,
                    IsDevice = false,
                    Name = tempFileName,
                };

                // 1. Setup Open to return true (file opened successfully)
                videoCaptureMock.Setup(v => v.Open(input.FileName)).Returns(true);

                // 2. Setup Read to simulate processing one frame, then returning false to stop the loop
                var frameCount = 0;
                videoCaptureMock.Setup(v => v.Read(It.IsAny<Mat>())).Returns((Mat mat) =>
                {
                    if (frameCount == 0)
                    {
                        // Create a small dummy image matrix to bypass the !currentFrame.Empty() check
                        var dummyImage = new Mat(10, 10, MatType.CV_8UC3, Scalar.Black);
                        dummyImage.CopyTo(mat);
                        frameCount++;
                        return true;
                    }

                    // Return false on the second call to break the do-while loop in CaptureAsync
                    return false;
                });

                // Act
                await videoService.RunAsync(input);

                // Assert
                // Ensure the input started event was called
                inputStartedEventMock.Verify(e => e.Publish(input), Times.Once);

                // Ensure the image frame was read at least once
                videoCaptureMock.Verify(v => v.Read(It.IsAny<Mat>()), Times.AtLeastOnce);

                // Ensure the loop ended and cleaned up properly
                Assert.False(videoService.IsActive);
                Assert.True(videoService.IsEnded);
                inputEndedEventMock.Verify(e => e.Publish(input), Times.Once);
            }
            finally
            {
                // Cleanup: Delete the temporary file after the test
                if (System.IO.File.Exists(tempFileName))
                {
                    System.IO.File.Delete(tempFileName);
                }
            }
        }

        [Fact]
        public void Stop_WhenCalledMultipleTimes_DoesNotThrow()
        {
            videoService.Stop();
            var exception = Record.Exception(() => videoService.Stop());

            Assert.Null(exception);
        }

        [Fact]
        public void Stop_WhenNeverStarted_DoesNotThrow()
        {
            var exception = Record.Exception(() => videoService.Stop());
            Assert.Null(exception);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                { }

                isDisposed = true;
            }
        }

        #endregion Protected Methods
    }
}