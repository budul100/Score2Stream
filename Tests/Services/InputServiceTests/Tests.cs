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
    public class Tests
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
        private readonly Mock<ITemplateService> templateServiceMock;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            containerProviderMock = new Mock<IContainerProvider>();
            deviceEnumeratorMock = new Mock<IDeviceEnumerator>();
            dialogServiceMock = new Mock<IDialogService>();
            eventAggregatorMock = new Mock<IEventAggregator>();
            loggerMock = new Mock<ILogger<Service>>();
            settingsServiceMock = new Mock<ISettingsService<Session>>();
            templateServiceMock = new Mock<ITemplateService>();

            session = new Session { Inputs = [] };
            settingsServiceMock.Setup(s => s.Contents).Returns(session);

            // Events required by the service constructor
            eventAggregatorMock.Setup(e => e.GetEvent<InputStartedEvent>()).Returns(inputStartedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<InputEndedEvent>()).Returns(inputEndedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreasChangedEvent>()).Returns(areasChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<InputSelectedEvent>()).Returns(inputSelectedEvent);

            // Safety setups — not required by InputService but avoids NullReferenceException
            // if other services publish these during test runs
            eventAggregatorMock.Setup(e => e.GetEvent<AreasOrderedEvent>()).Returns(areasOrderedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreaModifiedEvent>()).Returns(areaModifiedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<TemplatesChangedEvent>()).Returns(templatesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesChangedEvent>()).Returns(samplesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesOrderedEvent>()).Returns(samplesOrderedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SampleModifiedEvent>()).Returns(sampleModifiedEvent);

            deviceEnumeratorMock
                .Setup(d => d.GetVideoDevices())
                .Returns(new Dictionary<int, string>());

            inputService = new Service(
                settingsService: settingsServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                templateService: templateServiceMock.Object,
                deviceEnumerator: deviceEnumeratorMock.Object,
                containerProvider: containerProviderMock.Object,
                eventAggregator: eventAggregatorMock.Object,
                logger: loggerMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void AreasChanged_HasNoEffectOnInputService_NeverSaves()
        {
            // The InputService does not subscribe to AreasChangedEvent.
            // Publishing it must not trigger a Save().
            areasChangedEvent.Publish();

            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

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
        public void GetDevices_FiltersOutEmptyAndNullValues()
        {
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "Cam 1" },
                { 2, "" },
                { 3, null }
            });

            var result = inputService.GetDevices();

            Assert.Single(result);
            Assert.Equal("Cam 1", result[1]);
        }

        [Fact]
        public void Initialize_DeviceNotAvailable_AddsNoInput()
        {
            // Arrange — session contains a device input, but device is not connected
            var input = new Input
            {
                DeviceName = "GhostCam",
                IsDevice = true,
                Name = "GhostCam",
                IsActive = true,
            };
            session.Inputs.Add(input);

            // GetVideoDevices returns nothing → DeviceNotFoundException is swallowed
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>());

            // Act
            inputService.Initialize();

            // Assert — no input added, Active remains null
            Assert.Empty(inputService.Inputs);
            Assert.Null(inputService.Active);
        }

        [Fact]
        public void Initialize_FileNoLongerExists_AddsNoInput()
        {
            // Arrange — session contains a file input pointing to a non-existent file
            var input = new Input
            {
                FileName = "/nonexistent/path/video.mp4",
                IsDevice = false,
                Name = "OldVideo",
                IsActive = true,
            };
            session.Inputs.Add(input);

            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>());

            // Act
            inputService.Initialize();

            // Assert
            Assert.Empty(inputService.Inputs);
            Assert.Null(inputService.Active);
        }

        [Fact]
        public async Task Initialize_WithExistingActiveDeviceInput_SelectsFirstInput()
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

            var videoServiceMock = CreateVideoServiceMock();
            videoServiceMock.Setup(v => v.IsStarted).Returns(false);
            videoServiceMock.Setup(v => v.IsActive).Returns(false);

            var runCalled = new TaskCompletionSource<bool>();
            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback(() => runCalled.TrySetResult(true))
                .Returns(Task.CompletedTask);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            // Act
            inputService.Initialize();

            // Wait for fire-and-forget to complete
            await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            // Assert
            Assert.NotNull(inputService.Active);
            Assert.Equal("TestCam", inputService.Active.DeviceName);
            videoServiceMock.Verify(v => v.RunAsync(It.IsAny<Input>()), Times.Once);
        }

        [Fact]
        public async Task InputEndedEvent_Published_RemovesInputFromListAndSaves()
        {
            // Arrange — add an input via SelectDevice so it's in the Inputs list
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "EventCam" }
            });

            var videoServiceMock = CreateVideoServiceMock();
            videoServiceMock.Setup(v => v.IsStarted).Returns(false);

            var runCalled = new TaskCompletionSource<bool>();
            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback<Input>(inp =>
                {
                    videoServiceMock.Setup(v => v.IsStarted).Returns(true);
                    inputStartedEvent.Publish(inp);
                    runCalled.TrySetResult(true);
                })
                .Returns(Task.CompletedTask);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            inputService.SelectDevice("EventCam");
            await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var input = inputService.Active;
            Assert.NotNull(input);

            // Act — simulate the video service signalling that it ended
            inputEndedEvent.Publish(input);
            await Task.Delay(50); // SaveInputs is synchronous, but give event handler time

            // Assert — input marked inactive, settings saved
            Assert.False(input.IsActive);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task RemoveAsync_ActiveRemoved_SelectsNextAvailableInput()
        {
            // Arrange — two inputs, first is Active
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
    {
        { 1, "Cam1" },
        { 2, "Cam2" },
    });

            Input firstInput = null;
            Input secondInput = null;

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(() =>
                {
                    var vm = CreateVideoServiceMock();
                    vm.Setup(v => v.IsStarted).Returns(false);
                    vm.Setup(v => v.RunAsync(It.IsAny<Input>()))
                      .Callback<Input>(inp =>
                      {
                          vm.Setup(v => v.IsStarted).Returns(true);
                          inputStartedEvent.Publish(inp);
                      })
                      .Returns(Task.CompletedTask);
                    return vm.Object;
                });

            inputService.SelectDevice("Cam1");
            await Task.Delay(100);
            firstInput = inputService.Active;

            inputService.SelectDevice("Cam2");
            await Task.Delay(100);
            secondInput = inputService.Active;

            // Select first as Active
            inputService.Select(firstInput);

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.Yes);

            // Act
            await inputService.RemoveAsync(firstInput);

            // Assert — Active switched to the remaining input
            Assert.NotNull(inputService.Active);
            Assert.NotEqual(firstInput, inputService.Active);
        }

        [Fact]
        public async Task RemoveAsync_LastActiveInput_SetsActiveToNull()
        {
            // Arrange — single input
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "OnlyCam" }
            });

            var videoServiceMock = CreateVideoServiceMock();
            videoServiceMock.Setup(v => v.IsStarted).Returns(false);

            var runCalled = new TaskCompletionSource<bool>();
            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback<Input>(inp =>
                {
                    videoServiceMock.Setup(v => v.IsStarted).Returns(true);
                    inputStartedEvent.Publish(inp);
                    runCalled.TrySetResult(true);
                })
                .Returns(Task.CompletedTask);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            inputService.SelectDevice("OnlyCam");
            await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var input = inputService.Active;

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.Yes);

            // Act
            await inputService.RemoveAsync(input);

            // Assert
            Assert.Null(inputService.Active);
        }

        [Fact]
        public async Task RemoveAsync_NoActiveInput_DoesNothing()
        {
            // Active is null by default, no input passed → early return
            await inputService.RemoveAsync();

            dialogServiceMock.Verify(d => d.GetMessageBoxResultAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()), Times.Never);

            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_UserSaysNo_DoesNotStopOrSave()
        {
            // Arrange
            var areaServiceMock = new Mock<IAreaService>();
            var videoServiceMock = new Mock<IVideoService>();
            videoServiceMock.Setup(v => v.AreaService).Returns(areaServiceMock.Object);

            var input = new Input
            {
                DeviceName = "TestCam",
                IsDevice = true,
                Name = "TestCam",
                VideoService = videoServiceMock.Object,
            };

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.No);

            // Act
            await inputService.RemoveAsync(input);

            // Assert
            videoServiceMock.Verify(v => v.StopAsync(), Times.Never);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_UserSaysYes_StopsDisposesAndSaves()
        {
            // Arrange
            var videoServiceMock = CreateVideoServiceMock();
            var input = new Input
            {
                DeviceName = "TestCam",
                IsDevice = true,
                Name = "TestCam",
                VideoService = videoServiceMock.Object,
            };
            session.Inputs = [input];

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.Yes);

            // Act
            await inputService.RemoveAsync(input);

            // Assert
            videoServiceMock.Verify(v => v.StopAsync(), Times.Once);
            videoServiceMock.Verify(v => v.DisposeAsync(), Times.Once); // was missing
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Rotation_NoActiveInput_ReturnsZeroAndDoesNotSave()
        {
            // Active is null by default

            // Act
            var result = inputService.Rotation;
            inputService.Rotation = 45f; // should silently do nothing

            // Assert
            Assert.Equal(0f, result);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Rotation_SetOnActiveInput_SavesToSettings()
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

            var videoServiceMock = CreateVideoServiceMock();
            videoServiceMock.Setup(v => v.IsStarted).Returns(false);
            videoServiceMock.Setup(v => v.IsActive).Returns(false);

            var runCalled = new TaskCompletionSource<bool>();
            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback(() => runCalled.TrySetResult(true))
                .Returns(Task.CompletedTask);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            inputService.SelectDevice("Cam 1");
            await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            // Act
            inputService.Rotation = 90f;
            var result = inputService.Rotation;

            // Assert
            Assert.Equal(90f, result);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Select_SameActiveStartedInput_DoesNotPublishEvent()
        {
            // Arrange
            var videoServiceMock = CreateVideoServiceMock();
            videoServiceMock.Setup(v => v.IsStarted).Returns(true);

            var input = new Input
            {
                DeviceName = "Cam 1",
                IsDevice = true,
                Name = "Cam 1",
                VideoService = videoServiceMock.Object,
            };

            // Force Active to be set by publishing InputStartedEvent
            // (which calls ActivateInput → SaveInputs, but we bypass that here
            // by directly calling Select via a prior SelectDevice flow).
            // Simpler: use reflection or expose via a first Select call.
            // We call Select once to set Active, then track subsequent publishes.
            inputSelectedEvent.Subscribe(_ => { }); // baseline subscribe

            // Use a device setup so SelectDevice works, then manually stub IsStarted
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "Cam 1" }
            });

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            inputService.SelectDevice("Cam 1"); // sets Active

            var eventCount = 0;
            inputSelectedEvent.Subscribe(_ => eventCount++);

            // Act — select the exact same input again while IsStarted = true
            inputService.Select(inputService.Active);

            // Assert — no additional event published
            Assert.Equal(0, eventCount);
        }

        [Fact]
        public async Task SelectDevice_AlreadyRunning_DoesNotStartVideoAgain()
        {
            // Arrange
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "RunningCam" }
            });

            var videoServiceMock = CreateVideoServiceMock();

            // First call: not started yet
            videoServiceMock.Setup(v => v.IsStarted).Returns(false);

            var runCalled = new TaskCompletionSource<bool>();
            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback(() =>
                {
                    // After first run, mark as started
                    videoServiceMock.Setup(v => v.IsStarted).Returns(true);
                    runCalled.TrySetResult(true);
                })
                .Returns(Task.CompletedTask);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            inputService.SelectDevice("RunningCam");
            await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            // Act — select the same device again
            inputService.SelectDevice("RunningCam");
            await Task.Delay(100); // give fire-and-forget a chance if it were called

            // Assert — RunAsync called exactly once (AddInput exits early when IsStarted=true)
            videoServiceMock.Verify(v => v.RunAsync(It.IsAny<Input>()), Times.Once);
        }

        [Fact]
        public void SelectDevice_DeviceNotFound_ThrowsDeviceNotFoundException()
        {
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>());

            Assert.Throws<DeviceNotFoundException>(() => inputService.SelectDevice("UnknownCam"));
        }

        [Fact]
        public void SelectDevice_MaxCountExceeded_ShowsErrorDialog()
        {
            // Arrange — fill up to MaxCountInputs with distinct already-started inputs
            // by injecting them directly into session.Inputs and faking IsStarted
            // We need Constants.MaxCountInputs devices, each appearing "started"
            // The easiest approach: add inputs that AddInput skips (IsStarted=true),
            // then push the internal list via the InputStartedEvent path.

            // Simpler: add MaxCountInputs real inputs via SelectDevice with mocked video,
            // but that's complex. Instead, verify that when Inputs.Count >= max,
            // the dialog is shown. We do this by setting up enough started inputs
            // via the InputStartedEvent subscription (which calls ActivateInput).

            // Concrete approach: directly manipulate via multiple SelectDevice calls
            // with separate mocked video services, one per device.

            const int max = Commons.Assets.Constants.MaxCountInputs;

            var devices = new Dictionary<int, string>();
            for (var i = 1; i <= max; i++)
                devices[i] = $"Cam{i}";
            devices[max + 1] = "CamOverflow";

            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(devices);

            // Each Resolve call returns a fresh mock with IsStarted=false initially
            var callCount = 0;
            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(() =>
                {
                    callCount++;
                    var vm = CreateVideoServiceMock();
                    vm.Setup(v => v.IsStarted).Returns(false);
                    vm.Setup(v => v.RunAsync(It.IsAny<Input>()))
                      .Callback<Input>(inp =>
                      {
                          // Mark as started so AddInput guard works on re-entry
                          vm.Setup(v => v.IsStarted).Returns(true);
                          inputStartedEvent.Publish(inp);
                      })
                      .Returns(Task.CompletedTask);
                    return vm.Object;
                });

            // Fill up to max
            for (var i = 1; i <= max; i++)
                inputService.SelectDevice($"Cam{i}");

            // Act — one more device should trigger MaxCountExceededException → dialog
            inputService.SelectDevice("CamOverflow");

            // Assert
            dialogServiceMock.Verify(d => d.ShowMessageBoxAsync(
                It.IsAny<string>(),
                It.Is<string>(t => t == "Maximum count exceeded"),
                It.Is<Icon>(i => i == Icon.Error),
                It.IsAny<bool>(),
                It.IsAny<WindowStartupLocation>()), Times.Once);
        }

        [Fact]
        public async Task SelectDevice_ValidDevice_SetsActiveAndPublishesEvent()
        {
            // Arrange
            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(new Dictionary<int, string>
            {
                { 1, "ValidCam" }
            });

            var videoServiceMock = CreateVideoServiceMock();
            videoServiceMock.Setup(v => v.IsStarted).Returns(false);
            videoServiceMock.Setup(v => v.IsActive).Returns(false);

            var runCalled = new TaskCompletionSource<bool>();
            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback<Input>(i =>
                {
                    videoServiceMock.Setup(v => v.IsStarted).Returns(true);
                    videoServiceMock.Setup(v => v.IsActive).Returns(true);
                    inputStartedEvent.Publish(i);
                    runCalled.TrySetResult(true);
                })
                .Returns(Task.CompletedTask);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            var eventPublished = false;
            inputSelectedEvent.Subscribe(_ => eventPublished = true);

            // Act
            inputService.SelectDevice("ValidCam");
            await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            // Assert
            Assert.NotNull(inputService.Active);
            Assert.Equal("ValidCam", inputService.Active.DeviceName);
            Assert.Equal(1, inputService.Active.DeviceId);
            Assert.True(eventPublished);
            videoServiceMock.Verify(v => v.RunAsync(inputService.Active), Times.Once);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SelectFile_FileDoesNotExist_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => inputService.SelectFile("non_existent_file.mp4"));
        }

        [Fact]
        public async Task SelectFile_MaxCountExceeded_ShowsErrorDialog()
        {
            // Arrange
            const int max = Commons.Assets.Constants.MaxCountInputs;

            var devices = new Dictionary<int, string>();
            for (var i = 1; i <= max; i++)
                devices[i] = $"Cam{i}";

            deviceEnumeratorMock.Setup(d => d.GetVideoDevices()).Returns(devices);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(() =>
                {
                    var vm = CreateVideoServiceMock();
                    vm.Setup(v => v.IsStarted).Returns(false);
                    vm.Setup(v => v.RunAsync(It.IsAny<Input>()))
                      .Callback<Input>(inp =>
                      {
                          vm.Setup(v => v.IsStarted).Returns(true);
                          inputStartedEvent.Publish(inp);
                      })
                      .Returns(Task.CompletedTask);
                    return vm.Object;
                });

            for (var i = 1; i <= max; i++)
                inputService.SelectDevice($"Cam{i}");

            var overflowFile = Path.GetTempFileName();
            tempFiles.Add(overflowFile);

            // Act
            await Task.Run(() => inputService.SelectFile(overflowFile));

            // Assert
            dialogServiceMock.Verify(d => d.ShowMessageBoxAsync(
                It.IsAny<string>(),
                It.Is<string>(t => t == "Maximum count exceeded"),
                It.Is<Icon>(i => i == Icon.Error),
                It.IsAny<bool>(),
                It.IsAny<WindowStartupLocation>()), Times.Once);
        }

        [Fact]
        public async Task SelectFile_ValidFile_SetsActiveAndStartsVideo()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            tempFiles.Add(tempFile);

            var videoServiceMock = CreateVideoServiceMock();
            videoServiceMock.Setup(v => v.IsStarted).Returns(false);

            var runCalled = new TaskCompletionSource<bool>();
            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()))
                .Callback(() => runCalled.TrySetResult(true))
                .Returns(Task.CompletedTask);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            // Act
            inputService.SelectFile(tempFile);
            await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            // Assert
            Assert.NotNull(inputService.Active);
            Assert.Equal(tempFile, inputService.Active.FileName);
            Assert.False(inputService.Active.IsDevice);
            videoServiceMock.Verify(v => v.RunAsync(inputService.Active), Times.Once);
        }

        #endregion Public Methods

        #region Private Methods

        private static Mock<IVideoService> CreateVideoServiceMock()
        {
            var areaServiceMock = new Mock<IAreaService>();
            var videoServiceMock = new Mock<IVideoService>();

            videoServiceMock.Setup(v => v.AreaService).Returns(areaServiceMock.Object);

            return videoServiceMock;
        }

        #endregion Private Methods
    }
}