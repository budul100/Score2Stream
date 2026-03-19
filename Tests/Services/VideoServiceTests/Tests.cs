using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Media.Imaging;
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
        private readonly Mock<ITemplateService> templateServiceMock; // Fix: was missing
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
            templateServiceMock = new Mock<ITemplateService>(); // Fix: added
            videoCaptureMock = new Mock<IVideoCapture>();

            inputEndedEventMock = new Mock<InputEndedEvent>();
            inputStartedEventMock = new Mock<InputStartedEvent>();
            inputUpdatedEventMock = new Mock<InputUpdatedEvent>();
            segmentDrawnEventMock = new Mock<SegmentDrawnEvent>();
            segmentUpdatedEventMock = new Mock<SegmentUpdatedEvent>();
            areaModifiedEventMock = new Mock<AreaModifiedEvent>();

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

            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(It.IsAny<Action>(), It.IsAny<CancellationToken>()))
                .Callback<Action, CancellationToken>((action, _) => action())
                .Returns(Task.CompletedTask);

            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(It.IsAny<Func<Bitmap>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Func<Bitmap> f, CancellationToken _) => f());

            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(It.IsAny<Func<object>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Func<object> f, CancellationToken _) => f());

            // Fix: constructor parameter order now matches Service's signature;
            //      templateService added
            videoService = new Service(
                settingsService: settingsServiceMock.Object,
                areaService: areaServiceMock.Object,
                templateService: templateServiceMock.Object,
                videoCaptureFactory: () => videoCaptureMock.Object,
                dispatcherService: dispatcherServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object,
                recognitionService: recognitionServiceMock.Object,
                logger: loggerMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void Constructor_InitialState_IsCorrect()
        {
            Assert.False(videoService.IsActive);
            Assert.False(videoService.IsStarted);
            Assert.Null(videoService.Name);
            Assert.Null(videoService.Bitmap);
            Assert.Null(videoService.ProcessingTime);
        }

        [Fact]
        public void Constructor_SetsAreaServiceProperty()
        {
            Assert.NotNull(videoService.AreaService);
            Assert.Equal(areaServiceMock.Object, videoService.AreaService);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task Dispose_AfterStop_DoesNotThrow()
        {
            await videoService.StopAsync();
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
            var input = new Input
            {
                DeviceId = 999,
                DeviceName = "TestDevice",
                IsDevice = true,
                Name = "TestDevice",
            };

            videoCaptureMock.Setup(v => v.Read(It.IsAny<Mat>())).Returns(() =>
            {
                Task.Delay(500).Wait();
                return true;
            });

            var runTask = videoService.RunAsync(input);
            await Task.Delay(50);

            var disposeTask = Task.Run(() => ((IDisposable)videoService).Dispose());

            var completed = await Task.WhenAny(
                disposeTask,
                Task.Delay(TimeSpan.FromSeconds(5)));

            Assert.Equal(disposeTask, completed);
        }

        [Fact]
        public async Task RunAsync_SetsNameProperty()
        {
            var input = new Input
            {
                FileName = "dummy.mp4",
                IsDevice = false,
                Name = "dummy",
            };

            await videoService.RunAsync(input);

            // Fix: assert the actual expected value, not just non-null
            Assert.Equal("dummy", videoService.Name);
        }

        [Fact]
        public async Task RunAsync_WhileAlreadyRunning_SecondCallIsIgnored()
        {
            var input = new Input
            {
                DeviceId = 999,
                DeviceName = "TestDevice",
                IsDevice = true,
                Name = "TestDevice",
            };

            var firstRun = videoService.RunAsync(input);
            await Task.Delay(100);
            var secondRun = videoService.RunAsync(input);

            await videoService.StopAsync();
            await Task.WhenAll(firstRun, secondRun);

            inputStartedEventMock.Verify(e => e.Publish(It.IsAny<Input>()), Times.AtMostOnce);
        }

        [Fact]
        public async Task RunAsync_WithNonExistentFile_LogsError()
        {
            var input = new Input
            {
                FileName = "nonexistent_file_xyz.mp4",
                IsDevice = false,
                Name = "nonexistent_file_xyz",
            };

            // Note: no video.Open mock needed — service throws FileNotFoundException
            //       before Open is ever called when the file does not exist.

            await videoService.RunAsync(input);

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
            var tempFileName = System.IO.Path.GetTempFileName();

            try
            {
                var input = new Input
                {
                    FileName = tempFileName,
                    IsDevice = false,
                    Name = tempFileName,
                };

                videoCaptureMock.Setup(v => v.Open(input.FileName)).Returns(true);

                var frameCount = 0;
                videoCaptureMock.Setup(v => v.Read(It.IsAny<Mat>())).Returns((Mat mat) =>
                {
                    if (frameCount == 0)
                    {
                        var dummyImage = new Mat(10, 10, MatType.CV_8UC3, Scalar.Black);
                        dummyImage.CopyTo(mat);
                        frameCount++;
                        return true;
                    }
                    return false;
                });

                await videoService.RunAsync(input);

                inputStartedEventMock.Verify(e => e.Publish(input), Times.Once);
                videoCaptureMock.Verify(v => v.Read(It.IsAny<Mat>()), Times.AtLeastOnce);
                Assert.False(videoService.IsActive);
                Assert.False(videoService.IsStarted);
                inputEndedEventMock.Verify(e => e.Publish(input), Times.Once);
            }
            finally
            {
                if (System.IO.File.Exists(tempFileName))
                    System.IO.File.Delete(tempFileName);
            }
        }

        [Fact]
        public async Task StopAsync_WhenCalledMultipleTimes_DoesNotThrow()
        {
            await videoService.StopAsync();
            var exception = await Record.ExceptionAsync(() => videoService.StopAsync());

            Assert.Null(exception);
        }

        [Fact]
        public async Task StopAsync_WhenNeverStarted_DoesNotThrow()
        {
            var exception = await Record.ExceptionAsync(() => videoService.StopAsync());

            Assert.Null(exception);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    // Fix: actually dispose the service under test
                    ((IDisposable)videoService).Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods
    }
}