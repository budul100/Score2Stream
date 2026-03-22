using System.Threading.Tasks;
using Moq;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.Tests.MenuModuleTests.Base;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    [Collection("HeadlessUI")]
    public class SettingsPropertyTests(HeadlessSessionFixture fixture)
        : TestBase(fixture)
    {
        #region Public Methods

        [Fact]
        public async Task AllowMultipleInstances_Set_SameValue_DoesNotSave()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                session.App.AllowMultipleInstances = false;
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);

                viewModel.AllowMultipleInstances = false; // same value

                settingsServiceMock.Verify(m => m.Save(It.IsAny<string>()), Times.Never);
            });
        }

        [Fact]
        public async Task AllowMultipleInstances_Set_ToTrue_PersistsAndSaves()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                session.App.AllowMultipleInstances = false;
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);

                viewModel.AllowMultipleInstances = true;

                Assert.True(session.App.AllowMultipleInstances);
                settingsServiceMock.Verify(m => m.Save(It.IsAny<string>()), Times.AtLeastOnce);
            });
        }

        [Fact]
        public async Task IsSampleDetection_SetTrue_WhenInputIsNotActive_HasNoEffect()
        {
            await RunInSessionAsync(() =>
            {
                var sampleServiceMock = new Mock<ISampleService>();
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.SampleService).Returns(sampleServiceMock.Object);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.IsActive).Returns(false);

                var (viewModel, _, _, _, _, _, _) = CreateViewModel(
                    inputServiceMock: inputServiceMock,
                    templateServiceMock: templateServiceMock);

                viewModel.IsSampleDetection = true;

                // IsActive == false → setter guard fires → no change
                sampleServiceMock.VerifySet(s => s.IsDetection = It.IsAny<bool>(), Times.Never);
            });
        }

        [Fact]
        public async Task IsVerifiedsFiltered_Set_ToTrue_PersistsValue()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                session.Detection.FilterVerifieds = false;
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);

                viewModel.IsVerifiedsFiltered = true;

                Assert.True(session.Detection.FilterVerifieds);
            });
        }

        [Fact]
        public async Task PortServer_Set_AboveMax_IsIgnored()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);
                var originalValue = session.Server.PortServer;

                viewModel.PortServer = Constants.PortMax + 1;

                Assert.Equal(originalValue, session.Server.PortServer);
            });
        }

        [Fact]
        public async Task PortServer_Set_BelowMin_IsIgnored()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);
                var originalValue = session.Server.PortServer;

                viewModel.PortServer = Constants.PortMin - 1;

                Assert.Equal(originalValue, session.Server.PortServer);
                settingsServiceMock.Verify(m => m.Save(It.IsAny<string>()), Times.Never);
            });
        }

        [Fact]
        public async Task PortServer_Set_ValidValue_PersistsAndSaves()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);
                var validPort = Constants.PortMin + 1;

                viewModel.PortServer = validPort;

                Assert.Equal(validPort, session.Server.PortServer);
                settingsServiceMock.Verify(m => m.Save(It.IsAny<string>()), Times.AtLeastOnce);
            });
        }

        [Fact]
        public async Task ThresholdDetecting_Set_AboveMax_IsIgnored()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);
                var originalValue = session.Detection.ThresholdDetecting;

                viewModel.ThresholdDetecting = Constants.ThresholdMax + 1;

                Assert.Equal(originalValue, session.Detection.ThresholdDetecting);
                settingsServiceMock.Verify(m => m.Save(It.IsAny<string>()), Times.Never);
            });
        }

        [Fact]
        public async Task ThresholdDetecting_Set_BelowZero_IsIgnored()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);
                var originalValue = session.Detection.ThresholdDetecting;

                viewModel.ThresholdDetecting = -1;

                Assert.Equal(originalValue, session.Detection.ThresholdDetecting);
            });
        }

        [Fact]
        public async Task ThresholdDetecting_Set_ValidValue_PersistsAndSaves()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);

                viewModel.ThresholdDetecting = Constants.ThresholdMax / 2;

                Assert.Equal(Constants.ThresholdMax / 2, session.Detection.ThresholdDetecting);
                settingsServiceMock.Verify(m => m.Save(It.IsAny<string>()), Times.AtLeastOnce);
            });
        }

        [Fact]
        public async Task ThresholdMatching_Set_ValidValue_PersistsAndSaves()
        {
            await RunInSessionAsync(() =>
            {
                var session = new Session();
                var settingsServiceMock = new Mock<ISettingsService<Session>>();
                settingsServiceMock.Setup(m => m.Contents).Returns(session);

                var (viewModel, _, _, _, _, _, _) =
                    CreateViewModel(settingsServiceMock: settingsServiceMock);

                viewModel.ThresholdMatching = Constants.ThresholdMax / 2;

                Assert.Equal(Constants.ThresholdMax / 2, session.Detection.ThresholdMatching);
                settingsServiceMock.Verify(m => m.Save(It.IsAny<string>()), Times.AtLeastOnce);
            });
        }

        #endregion Public Methods
    }
}