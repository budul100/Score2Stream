using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Moq;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.InputService;
using Xunit;

namespace Score2Stream.Tests.InputServiceTests
{
    public class Tests
    {
        #region Private Fields

        private readonly AreaModifiedEvent areaModifiedEvent;
        private readonly AreasChangedEvent areasChangedEvent;
        private readonly AreasOrderedEvent areasOrderedEvent;
        private readonly Mock<IContainerProvider> containerProviderMock;
        private readonly Mock<IInputEnumerator> deviceEnumeratorMock;
        private readonly Mock<IDialogService> dialogServiceMock;
        private readonly Mock<IEventAggregator> eventAggregatorMock;
        private readonly InputsChangedEvent inputsChangedEvent;
        private readonly InputSelectedEvent inputSelectedEvent;
        private readonly Service inputService;
        private readonly SampleModifiedEvent sampleModifiedEvent;
        private readonly SamplesChangedEvent samplesChangedEvent;
        private readonly SamplesOrderedEvent samplesOrderedEvent;
        private readonly Session session;
        private readonly Mock<ISettingsService<Session>> settingsServiceMock;
        private readonly TemplatesChangedEvent templatesChangedEvent;
        private readonly InputEndedEvent videoEndedEvent;
        private readonly Mock<IVideoService> videoServiceMock;
        private readonly InputStartedEvent videoStartedEvent;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            containerProviderMock = new Mock<IContainerProvider>();
            dialogServiceMock = new Mock<IDialogService>();
            deviceEnumeratorMock = new Mock<IInputEnumerator>();
            settingsServiceMock = new Mock<ISettingsService<Session>>();
            eventAggregatorMock = new Mock<IEventAggregator>();
            videoServiceMock = new Mock<IVideoService>();

            videoStartedEvent = new InputStartedEvent();
            videoEndedEvent = new InputEndedEvent();
            areasChangedEvent = new AreasChangedEvent();
            areasOrderedEvent = new AreasOrderedEvent();
            areaModifiedEvent = new AreaModifiedEvent();
            templatesChangedEvent = new TemplatesChangedEvent();
            samplesChangedEvent = new SamplesChangedEvent();
            samplesOrderedEvent = new SamplesOrderedEvent();
            sampleModifiedEvent = new SampleModifiedEvent();
            inputsChangedEvent = new InputsChangedEvent();
            inputSelectedEvent = new InputSelectedEvent();

            eventAggregatorMock.Setup(e => e.GetEvent<InputStartedEvent>()).Returns(videoStartedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<InputEndedEvent>()).Returns(videoEndedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreasChangedEvent>()).Returns(areasChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreasOrderedEvent>()).Returns(areasOrderedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<AreaModifiedEvent>()).Returns(areaModifiedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<TemplatesChangedEvent>()).Returns(templatesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesChangedEvent>()).Returns(samplesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesOrderedEvent>()).Returns(samplesOrderedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SampleModifiedEvent>()).Returns(sampleModifiedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<InputsChangedEvent>()).Returns(inputsChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<InputSelectedEvent>()).Returns(inputSelectedEvent);

            session = new Session { Inputs = [] };
            settingsServiceMock.Setup(s => s.Contents).Returns(session);

            dialogServiceMock
                .Setup(d => d.GetFolderAsync(It.IsAny<string>(), It.IsAny<Environment.SpecialFolder?>()))
                .ReturnsAsync((IStorageFolder)null);

            containerProviderMock
                .Setup(c => c.Resolve(typeof(IVideoService)))
                .Returns(videoServiceMock.Object);

            videoServiceMock
                .Setup(v => v.RunAsync(It.IsAny<Input>()));

            // Configure IsActive to return true by default for testing
            videoServiceMock
                .Setup(v => v.IsActive)
                .Returns(true);

            // No devices by default
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>());

            inputService = new Service(
                settingsService: settingsServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                containerProvider: containerProviderMock.Object,
                inputEnumerator: deviceEnumeratorMock.Object,
                eventAggregator: eventAggregatorMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void Initialize_MatchesSettingsByName_NotByDeviceId()
        {
            // Arrange - Cam A was saved with DeviceId 0, now has DeviceId 1
            session.Inputs = [new(true) { DeviceId = 0, Name = "Cam A" }];

            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 1, "Cam A" },
                });

            // Act
            inputService.Initialize();

            // Assert - device is still recognized and set as active
            Assert.NotNull(inputService.Active);
            Assert.Equal("Cam A", inputService.Active.Name);
            Assert.Equal(1, inputService.Active.DeviceId);
        }

        [Fact]
        public void Initialize_NoDevices_InputsIsEmpty()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>());

            // Act
            inputService.Initialize();

            // Assert
            Assert.Empty(inputService.Inputs);
        }

        [Fact]
        public void Initialize_PublishesInputsChangedEvent()
        {
            // Arrange
            var published = false;
            inputsChangedEvent.Subscribe(() => published = true);

            // Act
            inputService.Initialize();

            // Assert
            Assert.True(published);
        }

        [Fact]
        public void Initialize_WithDevices_InputsContainsDevices()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                    { 1, "Cam B" },
                });

            // Act
            inputService.Initialize();

            // Assert
            Assert.Equal(2, inputService.Inputs.Count);
            Assert.Contains(inputService.Inputs, i => i.Name == "Cam A");
            Assert.Contains(inputService.Inputs, i => i.Name == "Cam B");
        }

        [Fact]
        public void SaveInputs_ContentUnchanged_DoesNotSave()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });

            session.Inputs = [new Input(true) { DeviceId = 0, Name = "Cam A" }];

            inputService.Initialize();
            settingsServiceMock.Invocations.Clear();

            // Act - no disconnect, no video event => no change
            inputService.Update();

            // Assert - no unnecessary save
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SelectAsync_DeviceInput_SetsActive()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            var input = inputService.Inputs.First();

            // Act
            await inputService.SelectAsync(input);

            // Assert
            Assert.Equal(input, inputService.Active);
        }

        [Fact]
        public async Task SelectAsync_NullInput_DoesNotChangeActive()
        {
            // Arrange
            inputService.Initialize();

            // Act
            await inputService.SelectAsync(null);

            // Assert
            Assert.Null(inputService.Active);
        }

        [Fact]
        public async Task StopAsync_NoActiveInputs_DoesNotShowDialog()
        {
            // Arrange
            inputService.Initialize();

            // Act
            await inputService.StopAsync();

            // Assert
            dialogServiceMock.Verify(
                d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    ButtonEnum.YesNo,
                    ClickEnum.Yes,
                    ClickEnum.No,
                    Icon.Question,
                    true,
                    WindowStartupLocation.CenterOwner),
                Times.Never);
        }

        [Fact]
        public async Task StopAsync_WithActiveInputs_ShowsConfirmationDialog()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });

            session.Inputs = [new Input(true) { DeviceId = 0, Name = "Cam A" }];

            inputService.Initialize();

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    ButtonEnum.YesNo,
                    ClickEnum.Yes,
                    ClickEnum.No,
                    Icon.Question,
                    true,
                    WindowStartupLocation.CenterOwner))
                .ReturnsAsync(ButtonResult.No);

            // Act
            await inputService.StopAsync();

            // Assert
            dialogServiceMock.Verify(
                d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    ButtonEnum.YesNo,
                    ClickEnum.Yes,
                    ClickEnum.No,
                    Icon.Question,
                    true,
                    WindowStartupLocation.CenterOwner),
                Times.Once);
        }

        [Fact]
        public void Update_ActiveDeviceDisconnected_ActiveIsNull()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });

            session.Inputs = [new Input(true) { DeviceId = 0, Name = "Cam A" }];

            inputService.Initialize();
            Assert.NotNull(inputService.Active);

            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>());

            // Act
            inputService.Update();

            // Assert
            Assert.Null(inputService.Active);
        }

        [Fact]
        public void Update_ChangedDevices_PublishesInputsChangedEvent()
        {
            // Arrange
            inputService.Initialize();

            var publishCount = 0;
            inputsChangedEvent.Subscribe(() => publishCount++);

            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert
            Assert.True(publishCount > 0);
        }

        [Fact]
        public void Update_DeviceDisconnected_IsRemovedFromInputs()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>());

            // Act
            inputService.Update();

            // Assert
            Assert.Empty(inputService.Inputs);
        }

        [Fact]
        public void Update_DeviceIdChangedWithoutDisconnect_DeviceIdIsUpdated()
        {
            // Arrange - device starts with index 0
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            // Device index shifts to 1 (e.g. another device was plugged in before it)
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 1, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert - DeviceId is updated without removing the input
            var input = Assert.Single(inputService.Inputs);
            Assert.Equal("Cam A", input.Name);
            Assert.Equal(1, input.DeviceId);
        }

        [Fact]
        public void Update_DeviceIdChangedWithoutDisconnect_PublishesInputsChangedEvent()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            var publishCount = 0;
            inputsChangedEvent.Subscribe(() => publishCount++);

            // Device index shifts without disconnect
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 1, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert
            Assert.Equal(1, publishCount);
        }

        [Fact]
        public void Update_DeviceReconnectedWithNewDeviceId_DeviceIdIsUpdated()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            // Device disconnects
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>());
            inputService.Update();

            // Device reconnects with a new index
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 1, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert - name stays stable, DeviceId is updated
            var input = Assert.Single(inputService.Inputs);
            Assert.Equal("Cam A", input.Name);
            Assert.Equal(1, input.DeviceId);
        }

        [Fact]
        public void Update_DeviceReconnectedWithNewDeviceId_NoDuplicateInputs()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 1, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert - no duplicate despite new DeviceId
            Assert.Single(inputService.Inputs);
        }

        [Fact]
        public void Update_MultipleDevicesSwapIndices_BothDeviceIdsAreUpdated()
        {
            // Arrange - two devices with initial indices
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                    { 1, "Cam B" },
                });
            inputService.Initialize();

            // Indices swap (e.g. due to USB enumeration order change)
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam B" },
                    { 1, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert - both DeviceIds are updated correctly
            Assert.Equal(2, inputService.Inputs.Count);
            Assert.Equal(1, inputService.Inputs.Single(i => i.Name == "Cam A").DeviceId);
            Assert.Equal(0, inputService.Inputs.Single(i => i.Name == "Cam B").DeviceId);
        }

        [Fact]
        public void Update_NewDeviceAddedWhileExistingRemains_BothPresent()
        {
            // Arrange - start with one device
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            // A second device is connected
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                    { 1, "Cam B" },
                });

            // Act
            inputService.Update();

            // Assert
            Assert.Equal(2, inputService.Inputs.Count);
            Assert.Contains(inputService.Inputs, i => i.Name == "Cam A" && i.DeviceId == 0);
            Assert.Contains(inputService.Inputs, i => i.Name == "Cam B" && i.DeviceId == 1);
        }

        [Fact]
        public void Update_NewDeviceConnected_IsAddedToInputs()
        {
            // Arrange
            inputService.Initialize();

            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert
            Assert.Single(inputService.Inputs);
            Assert.Contains(inputService.Inputs, i => i.Name == "Cam A");
        }

        [Fact]
        public void Update_NewDeviceConnected_PublishesInputsChangedEvent()
        {
            // Arrange
            inputService.Initialize();

            var publishCount = 0;
            inputsChangedEvent.Subscribe(() => publishCount++);

            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert
            Assert.Equal(1, publishCount);
        }

        [Fact]
        public void Update_NewDeviceShiftsExistingIndex_NoDuplicateAndIdUpdated()
        {
            // Arrange - Cam A starts at index 0
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            // Cam B is plugged in and takes index 0, Cam A shifts to index 1
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam B" },
                    { 1, "Cam A" },
                });

            // Act
            inputService.Update();

            // Assert - no duplicate, Cam A's DeviceId is updated
            Assert.Equal(2, inputService.Inputs.Count);
            Assert.Equal(1, inputService.Inputs.Single(i => i.Name == "Cam A").DeviceId);
            Assert.Equal(0, inputService.Inputs.Single(i => i.Name == "Cam B").DeviceId);
        }

        [Fact]
        public void Update_NoChanges_DoesNotPublishInputsChangedEvent()
        {
            // Arrange
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                });
            inputService.Initialize();

            var publishCount = 0;
            inputsChangedEvent.Subscribe(() => publishCount++);

            // Act - same devices, no disconnect/connect
            inputService.Update();

            // Assert
            Assert.Equal(0, publishCount);
        }

        [Fact]
        public void Update_OneOfMultipleDevicesDisconnected_OnlyThatOneRemoved()
        {
            // Arrange - two devices connected
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam A" },
                    { 1, "Cam B" },
                });
            inputService.Initialize();

            // Cam A is disconnected, Cam B remains
            deviceEnumeratorMock
                .Setup(d => d.GetDevices())
                .Returns(new Dictionary<int, string>
                {
                    { 0, "Cam B" },
                });

            // Act
            inputService.Update();

            // Assert
            Assert.Single(inputService.Inputs);
            Assert.Equal("Cam B", inputService.Inputs.Single().Name);
            Assert.Equal(0, inputService.Inputs.Single().DeviceId);
        }

        #endregion Public Methods
    }
}