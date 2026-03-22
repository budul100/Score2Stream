using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Moq;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.MenuModule.ViewModels;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    [Collection("HeadlessUI")]
    public class InputTests
        : TestBase
    {
        #region Public Methods

        [Fact]
        public async Task AreaAddCommand_CanExecute_ReturnsFalseWhenInputIsNotActive()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(false);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.False(viewModel.AreaAddCommand.CanExecute(null));
            });
        }

        [Fact]
        public async Task AreaAddCommand_CanExecute_ReturnsTrueWhenInputIsActive()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.True(viewModel.AreaAddCommand.CanExecute(null));
            });
        }

        [Fact]
        public async Task AreaAddCommand_Execute_CallsAreaServiceCreate_WithValidSegmentCount()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                var validCount = Constants.SegmentsCountMin.ToString();
                viewModel.AreaAddCommand.Execute(validCount);

                areaServiceMock.Verify(
                    m => m.Create(Constants.SegmentsCountMin),
                    Times.Once);
            });
        }

        [Fact]
        public async Task AreaAddCommand_Execute_DoesNotCallAreaServiceCreate_WhenCountAboveMax()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                var aboveMax = (Constants.SegmentsCountMax + 1).ToString();
                viewModel.AreaAddCommand.Execute(aboveMax);

                areaServiceMock.Verify(m => m.Create(It.IsAny<int>()), Times.Never);
            });
        }

        [Fact]
        public async Task AreaAddCommand_Execute_DoesNotCallAreaServiceCreate_WhenCountBelowMin()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                var belowMin = (Constants.SegmentsCountMin - 1).ToString();
                viewModel.AreaAddCommand.Execute(belowMin);

                areaServiceMock.Verify(m => m.Create(It.IsAny<int>()), Times.Never);
            });
        }

        [Fact]
        public async Task InputCenterCommand_CanExecute_ReturnsFalseWhenInputIsNotActive()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(false);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.False(viewModel.InputCenterCommand.CanExecute());
            });
        }

        [Fact]
        public async Task InputCenterCommand_CanExecute_ReturnsTrueWhenInputIsActive()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.True(viewModel.InputCenterCommand.CanExecute());
            });
        }

        [Fact]
        public async Task InputRefreshCommand_Execute_AlwaysAddsFileInputOption()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock
                    .Setup(m => m.GetDevices())
                    .Returns(new Dictionary<int, string>());

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputRefreshCommand.Execute();

                Assert.Single(viewModel.Inputs);
                Assert.Equal(Texts.MenuInputFileText, viewModel.Inputs[0].Text);
            });
        }

        [Fact]
        public async Task InputRefreshCommand_Execute_CallsGetDevices()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock
                    .Setup(m => m.GetDevices())
                    .Returns(new Dictionary<int, string>());

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputRefreshCommand.Execute();

                inputServiceMock.Verify(m => m.GetDevices(), Times.Once);
            });
        }

        [Fact]
        public async Task InputRefreshCommand_Execute_ClearsPreviousInputsFirst()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock
                    .Setup(m => m.GetDevices())
                    .Returns(new Dictionary<int, string> { { 0, "Camera 0" } });

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputRefreshCommand.Execute();
                viewModel.InputRefreshCommand.Execute();

                // Should not accumulate — same count on second call
                Assert.Equal(2, viewModel.Inputs.Count); // 1 device + 1 file
            });
        }

        [Fact]
        public async Task InputRefreshCommand_Execute_PopulatesInputsWithDevices()
        {
            await RunInSessionAsync(() =>
            {
                var devices = new Dictionary<int, string>
                {
                    { 0, "Camera 0" },
                    { 1, "Camera 1" },
                };

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.GetDevices()).Returns(devices);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputRefreshCommand.Execute();

                // Devices + 1 file input option
                Assert.Equal(devices.Count + 1, viewModel.Inputs.Count);
            });
        }

        [Fact]
        public async Task InputRotateLeftCommand_CanExecute_ReturnsFalseWhenNotActive()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(false);
                inputServiceMock.Setup(m => m.Rotation).Returns(Constants.RotateLeftMax);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.False(viewModel.InputRotateLeftCommand.CanExecute());
            });
        }

        [Fact]
        public async Task InputRotateLeftCommand_CanExecute_ReturnsFalseWhenRotationBelowLimit()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.Rotation).Returns(Constants.RotateLeftMax - 1);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.False(viewModel.InputRotateLeftCommand.CanExecute());
            });
        }

        [Fact]
        public async Task InputRotateLeftCommand_CanExecute_ReturnsTrueWhenActiveAndRotationAtLeftLimit()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.Rotation).Returns(Constants.RotateLeftMax);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.True(viewModel.InputRotateLeftCommand.CanExecute());
            });
        }

        [Fact]
        public async Task InputRotateLeftCommand_Execute_DecrementsRotationByStep()
        {
            await RunInSessionAsync(() =>
            {
                var currentRotation = Constants.RotateLeftMax;

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.Rotation).Returns(currentRotation);
                inputServiceMock
                    .SetupSet(m => m.Rotation = It.IsAny<float>())
                    .Callback<float>(v => currentRotation = v);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputRotateLeftCommand.Execute();

                Assert.Equal(Constants.RotateLeftMax - Constants.RotateStep, currentRotation);
            });
        }

        [Fact]
        public async Task InputRotateRightCommand_CanExecute_ReturnsFalseWhenRotationExceedsLimit()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.Rotation).Returns(Constants.RotateRightMax + 1);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.False(viewModel.InputRotateRightCommand.CanExecute());
            });
        }

        [Fact]
        public async Task InputRotateRightCommand_CanExecute_ReturnsTrueWhenActiveAndRotationAtRightLimit()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.Rotation).Returns(Constants.RotateRightMax);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.True(viewModel.InputRotateRightCommand.CanExecute());
            });
        }

        [Fact]
        public async Task InputRotateRightCommand_Execute_DoesNotRotateWhenCannotRotateRight()
        {
            await RunInSessionAsync(() =>
            {
                var currentRotation = Constants.RotateRightMax + Constants.RotateStep;

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.Rotation).Returns(currentRotation);
                inputServiceMock
                    .SetupSet(m => m.Rotation = It.IsAny<float>())
                    .Callback<float>(v => currentRotation = v);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputRotateRightCommand.Execute();

                // Rotation must not change
                Assert.Equal(Constants.RotateRightMax + Constants.RotateStep, currentRotation);
            });
        }

        [Fact]
        public async Task InputRotateRightCommand_Execute_IncrementsRotationByStep()
        {
            await RunInSessionAsync(() =>
            {
                var currentRotation = 0f;

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);
                inputServiceMock.Setup(m => m.Rotation).Returns(() => currentRotation);
                inputServiceMock
                    .SetupSet(m => m.Rotation = It.IsAny<float>())
                    .Callback<float>(v => currentRotation = v);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputRotateRightCommand.Execute();

                Assert.Equal(Constants.RotateStep, currentRotation);
            });
        }

        [Fact]
        public async Task InputSelectCommand_FilePickerCancelled_DoesNotCallSelectFile()
        {
            await RunInSessionAsync(async () =>
            {
                var dialogServiceMock = new Mock<IDialogService>();
                dialogServiceMock
                    .Setup(d => d.OpenFilePickerAsync(
                        It.IsAny<string>(),
                        default,
                        It.IsAny<bool>(),
                        It.IsAny<IStorageFolder>()))
                    .ReturnsAsync([]);

                var inputServiceMock = new Mock<IInputService>();

                var (viewModel, _) = CreateViewModel(inputServiceMock, dialogServiceMock);

                viewModel.InputSelectCommand.Execute(null);
                await Task.Yield();

                inputServiceMock.Verify(m => m.SelectFile(It.IsAny<string>()), Times.Never);
            });
        }

        [Fact]
        public async Task InputSelectCommand_MaxCountExceeded_DoesNotThrow()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock
                    .Setup(m => m.SelectDevice(It.IsAny<string>()))
                    .Throws(new MaxCountExceededException(typeof(Input), Constants.MaxCountInputs));

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                var exception = Record.Exception(
                    () => viewModel.InputSelectCommand.Execute("Camera 0"));

                Assert.Null(exception);
            });
        }

        [Fact]
        public async Task InputSelectCommand_MaxCountExceeded_ShowsErrorDialog()
        {
            await RunInSessionAsync(async () =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock
                    .Setup(m => m.SelectDevice(It.IsAny<string>()))
                    .Throws(new MaxCountExceededException(typeof(Input), Constants.MaxCountInputs));

                var dialogServiceMock = new Mock<IDialogService>();

                var (viewModel, _) = CreateViewModel(inputServiceMock, dialogServiceMock);

                viewModel.InputSelectCommand.Execute("Camera 0");
                await Task.Yield();

                dialogServiceMock.Verify(
                    m => m.ShowMessageBoxAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<MsBox.Avalonia.Enums.Icon>(),
                        It.IsAny<bool>(),
                        It.IsAny<WindowStartupLocation>()),
                    Times.Once);
            });
        }

        [Fact]
        public async Task InputSelectCommand_WithDeviceName_CallsSelectDevice()
        {
            await RunInSessionAsync(async () =>
            {
                var inputServiceMock = new Mock<IInputService>();
                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputSelectCommand.Execute("Camera 0");
                await Task.Yield();

                inputServiceMock.Verify(m => m.SelectDevice("Camera 0"), Times.Once);
            });
        }

        [Fact]
        public async Task InputSelectCommand_WithDeviceName_DoesNotCallSelectFile()
        {
            await RunInSessionAsync(async () =>
            {
                var inputServiceMock = new Mock<IInputService>();
                var (viewModel, _) = CreateViewModel(inputServiceMock);

                viewModel.InputSelectCommand.Execute("Camera 0");
                await Task.Yield();

                inputServiceMock.Verify(m => m.SelectFile(It.IsAny<string>()), Times.Never);
            });
        }

        [Fact]
        public async Task InputSelectCommand_WithNullDeviceName_CallsSelectFile()
        {
            await RunInSessionAsync(async () =>
            {
                var filePath = "/tmp/test_video.mp4";

                var storageMock = new Mock<IStorageFile>();
                storageMock.Setup(f => f.Path).Returns(new Uri(filePath));

                var dialogServiceMock = new Mock<IDialogService>();
                dialogServiceMock
                    .Setup(d => d.OpenFilePickerAsync(
                        It.IsAny<string>(),
                        default,
                        It.IsAny<bool>(),
                        It.IsAny<IStorageFolder>()))
                    .ReturnsAsync([storageMock.Object]);
                dialogServiceMock
                    .Setup(d => d.GetFolderAsync(
                        It.IsAny<string>(),
                        It.IsAny<Environment.SpecialFolder>()))
                    .ReturnsAsync((IStorageFolder)null);

                var inputServiceMock = new Mock<IInputService>();

                var (viewModel, _) = CreateViewModel(inputServiceMock, dialogServiceMock);

                viewModel.InputSelectCommand.Execute(default);
                await Task.Yield();

                inputServiceMock.Verify(m => m.SelectFile(It.IsAny<string>()), Times.Once);
            });
        }

        [Fact]
        public async Task IsActive_ReturnsFalse_WhenInputServiceIsNotActive()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(false);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.False(viewModel.IsActive);
            });
        }

        [Fact]
        public async Task IsActive_ReturnsTrue_WhenInputServiceIsActive()
        {
            await RunInSessionAsync(() =>
            {
                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(true);

                var (viewModel, _) = CreateViewModel(inputServiceMock);

                Assert.True(viewModel.IsActive);
            });
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Creates a MenuViewModel with a configurable inputService mock and returns both.
        /// </summary>
        private static (MenuViewModel ViewModel, Mock<IInputService> InputServiceMock) CreateViewModel(
            Mock<IInputService> inputServiceMock = null,
            Mock<IDialogService> dialogServiceMock = null,
            Mock<IEventAggregator> eventAggregatorMock = null)
        {
            inputServiceMock ??= new Mock<IInputService>();
            dialogServiceMock ??= new Mock<IDialogService>();
            eventAggregatorMock ??= CreateEventAggregatorMock();

            var session = new Session();
            var settingsServiceMock = new Mock<ISettingsService<Session>>();
            settingsServiceMock.Setup(m => m.Contents).Returns(session);

            var viewModel = new MenuViewModel(
                settingsService: settingsServiceMock.Object,
                webService: new Mock<IWebService>().Object,
                scoreboardService: new Mock<IScoreboardService>().Object,
                inputService: inputServiceMock.Object,
                templateService: new Mock<ITemplateService>().Object,
                regionManager: new Mock<IRegionManager>().Object,
                dialogService: dialogServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);

            return (viewModel, inputServiceMock);
        }

        #endregion Private Methods
    }
}