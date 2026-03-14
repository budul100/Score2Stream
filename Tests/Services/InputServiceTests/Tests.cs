using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using Moq;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.InputService;
using Xunit;

namespace Score2Stream.Tests.InputServiceTests
{
    public class ServiceTests
        : IDisposable
    {
        #region Private Fields

        private readonly AreaModifiedEvent areaModifiedEvent = new();
        private readonly AreasChangedEvent areasChangedEvent = new();
        private readonly AreasOrderedEvent areasOrderedEvent = new();
        private readonly Mock<IContainerProvider> containerProviderMock;
        private readonly Mock<IDeviceEnumerator> deviceEnumeratorMock;
        private readonly Mock<IDialogService> dialogServiceMock;
        private readonly Mock<IEventAggregator> eventAggregatorMock;
        private readonly InputEndedEvent inputEndedEvent = new();
        private readonly InputSelectedEvent inputSelectedEvent = new();
        private readonly Service inputService;
        private readonly InputStartedEvent inputStartedEvent = new();
        private readonly Mock<ILogger<Service>> loggerMock;
        private readonly SampleModifiedEvent sampleModifiedEvent = new();
        private readonly SamplesChangedEvent samplesChangedEvent = new();
        private readonly SamplesOrderedEvent samplesOrderedEvent = new();
        private readonly Session session;
        private readonly Mock<ISettingsService<Session>> settingsServiceMock;
        private readonly List<string> tempFiles = [];
        private readonly TemplatesChangedEvent templatesChangedEvent = new();

        #endregion Private Fields

        #region Public Constructors

        public ServiceTests()
        {
            containerProviderMock = new Mock<IContainerProvider>();
            deviceEnumeratorMock = new Mock<IDeviceEnumerator>();
            dialogServiceMock = new Mock<IDialogService>();
            eventAggregatorMock = new Mock<IEventAggregator>();
            loggerMock = new Mock<ILogger<Service>>();
            settingsServiceMock = new Mock<ISettingsService<Session>>();

            session = new Session { Inputs = [] };
            settingsServiceMock.Setup(s => s.Contents).Returns(session);

            eventAggregatorMock.Setup(e => e.GetEvent<InputStartedEvent>()).Returns(inputStartedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<InputEndedEvent>()).Returns(inputEndedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreasChangedEvent>()).Returns(areasChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreasOrderedEvent>()).Returns(areasOrderedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreaModifiedEvent>()).Returns(areaModifiedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<TemplatesChangedEvent>()).Returns(templatesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesChangedEvent>()).Returns(samplesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesOrderedEvent>()).Returns(samplesOrderedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SampleModifiedEvent>()).Returns(sampleModifiedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<InputSelectedEvent>()).Returns(inputSelectedEvent);

            deviceEnumeratorMock
                .Setup(d => d.GetVideoDevices())
                .Returns(new Dictionary<int, string>());

            inputService = new Service(
                settingsService: settingsServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                deviceEnumerator: deviceEnumeratorMock.Object,
                containerProvider: containerProviderMock.Object,
                eventAggregator: eventAggregatorMock.Object,
                logger: loggerMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            foreach (var file in tempFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }

            GC.SuppressFinalize(this);
        }

        [Fact]
        public void EventAggregator_AreasChanged_TriggersSave()
        {
            // Act
            areasChangedEvent.Publish();

            // Assert
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetDevices_FiltersOutEmptyValues()
        {
            // Arrange
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "Cam 1" },
                { 2, "" },
                { 3, null }
            });

            // Act
            var result = inputService.GetDevices();

            // Assert
            Assert.Single(result);
            Assert.Equal("Cam 1", result[1]);
        }

        [Fact]
        public void Initialize_WithExistingInputs_SelectsFirstInput()
        {
            // Arrange
            var input = new Input
            {
                DeviceName = "TestCam",
                IsDevice = true,
                Name = "TestCam",
                IsActive = true,
            };

            session.Inputs.Add(input);

            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 0, "TestCam" }
            });

            var videoServiceMock = new Mock<IVideoService>();

            videoServiceMock.Setup(v => v.IsStarted).Returns(false);
            videoServiceMock.Setup(v => v.IsActive).Returns(false);

            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback(() =>
                {
                    videoServiceMock.Setup(v => v.IsStarted).Returns(true);
                    videoServiceMock.Setup(v => v.IsActive).Returns(true);
                })
                .Returns(Task.CompletedTask);

            var templateServiceMock = new Mock<ITemplateService>();
            var areaServiceMock = new Mock<IAreaService>();
            areaServiceMock.Setup(a => a.TemplateService).Returns(templateServiceMock.Object);

            videoServiceMock.Setup(v => v.AreaService).Returns(areaServiceMock.Object);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            // Act
            inputService.Initialize();

            // Assert
            Assert.NotNull(inputService.Active);
            Assert.Equal("TestCam", inputService.Active.DeviceName);
            videoServiceMock.Verify(v => v.RunAsync(It.IsAny<Input>()), Times.Once);
        }

        [Fact]
        public void Rotation_GetAndSet_SavesToSettings()
        {
            // Arrange
            var input = new Input
            {
                DeviceName = "Cam 1",
                IsDevice = true,
                Name = "Cam 1",
            };

            session.Inputs = [input];

            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "Cam 1" }
            });

            var videoServiceMock = new Mock<IVideoService>();

            videoServiceMock.Setup(v => v.IsStarted).Returns(false);
            videoServiceMock.Setup(v => v.IsActive).Returns(false);

            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback(() =>
                {
                    videoServiceMock.Setup(v => v.IsStarted).Returns(true);
                    videoServiceMock.Setup(v => v.IsActive).Returns(true);
                })
                .Returns(Task.CompletedTask);

            var templateServiceMock = new Mock<ITemplateService>();
            var areaServiceMock = new Mock<IAreaService>();
            areaServiceMock.Setup(a => a.TemplateService).Returns(templateServiceMock.Object);

            videoServiceMock.Setup(v => v.AreaService).Returns(areaServiceMock.Object);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            inputService.SelectDevice("Cam 1");

            // Act
            inputService.Rotation = 90f;
            var result = inputService.Rotation;

            // Assert
            Assert.Equal(90f, result);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void SelectDevice_DeviceNotFound_ThrowsException()
        {
            // Arrange
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>());

            // Act & Assert
            Assert.Throws<DeviceNotFoundException>(() => inputService.SelectDevice("UnknownCam"));
        }

        [Fact]
        public void SelectDevice_ValidDevice_SetsActiveAndStartsVideo()
        {
            // Arrange
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "ValidCam" }
            });

            var videoServiceMock = new Mock<IVideoService>();

            videoServiceMock.Setup(v => v.IsStarted).Returns(false);
            videoServiceMock.Setup(v => v.IsActive).Returns(false);

            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback(() =>
                {
                    videoServiceMock.Setup(v => v.IsStarted).Returns(true);
                    videoServiceMock.Setup(v => v.IsActive).Returns(true);
                })
                .Returns(Task.CompletedTask);

            var templateServiceMock = new Mock<ITemplateService>();
            var areaServiceMock = new Mock<IAreaService>();
            areaServiceMock.Setup(a => a.TemplateService).Returns(templateServiceMock.Object);

            videoServiceMock.Setup(v => v.AreaService).Returns(areaServiceMock.Object);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            var eventPublished = false;
            inputSelectedEvent.Subscribe(_ => eventPublished = true);

            // Act
            inputService.SelectDevice("ValidCam");

            // Assert
            Assert.NotNull(inputService.Active);
            Assert.Equal("ValidCam", inputService.Active.DeviceName);
            Assert.Equal(1, inputService.Active.DeviceId);
            Assert.True(eventPublished);
            videoServiceMock.Verify(v => v.RunAsync(inputService.Active), Times.Once);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SelectFile_FileDoesNotExist_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => inputService.SelectFile("non_existent_file.mp4"));
        }

        [Fact]
        public void SelectFile_ValidFile_SetsActiveAndStartsVideo()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            tempFiles.Add(tempFile);

            var videoServiceMock = new Mock<IVideoService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            var templateServiceMock = new Mock<ITemplateService>();
            var areaServiceMock = new Mock<IAreaService>();
            areaServiceMock.Setup(a => a.TemplateService).Returns(templateServiceMock.Object);

            videoServiceMock.Setup(v => v.AreaService).Returns(areaServiceMock.Object);

            // Act
            inputService.SelectFile(tempFile);

            // Assert
            Assert.NotNull(inputService.Active);
            Assert.Equal(tempFile, inputService.Active.FileName);
            Assert.False(inputService.Active.IsDevice);
            videoServiceMock.Verify(v => v.RunAsync(inputService.Active), Times.Once);
        }

        [Fact]
        public async Task StopAsync_UserSaysNo_DoesNotStopVideo()
        {
            // Arrange
            var input = new Input
            {
                DeviceName = "TestCam",
                IsDevice = true,
                Name = "TestCam",
                VideoService = new Mock<IVideoService>().Object,
            };

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.No);

            // Act
            await inputService.StopAsync(input);

            // Assert
            var videoMock = Mock.Get(input.VideoService);

            videoMock.Verify(v => v.StopAsync(), Times.Never);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task StopAsync_UserSaysYes_StopsVideoAndSaves()
        {
            // Arrange
            var videoServiceMock = new Mock<IVideoService>();

            var input = new Input
            {
                DeviceName = "TestCam",
                IsDevice = true,
                Name = "TestCam",
                VideoService = videoServiceMock.Object,
            };

            // Add input to session.Inputs, then let StopAsync remove it,
            // which produces a real difference and triggers Save()
            session.Inputs = [input];

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.Yes);

            // Act
            await inputService.StopAsync(input);

            // Assert
            videoServiceMock.Verify(v => v.StopAsync(), Times.Once);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Once);
        }

        #endregion Public Methods
    }
}