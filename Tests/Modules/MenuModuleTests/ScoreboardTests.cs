using System.Threading.Tasks;
using Moq;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Tests.MenuModuleTests.Base;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    [Collection("HeadlessUI")]
    public class ScoreboardTests(HeadlessSessionFixture fixture)
        : TestBase(fixture)
    {
        #region Public Methods

        [Fact]
        public async Task IsUpToDate_ReflectsScoreboardService()
        {
            await RunInSessionAsync(() =>
            {
                var scoreboardServiceMock = new Mock<IScoreboardService>();
                scoreboardServiceMock.Setup(m => m.IsUpToDate).Returns(true);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(scoreboardServiceMock: scoreboardServiceMock);

                Assert.True(viewModel.IsUpToDate);
            });
        }

        [Fact]
        public async Task ScoreboardOpenCommand_CanExecute_ReturnsFalse_WhenWebServiceIsNotActive()
        {
            await RunInSessionAsync(() =>
            {
                var webServiceMock = new Mock<IWebService>();
                webServiceMock.Setup(m => m.IsActive).Returns(false);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(webServiceMock: webServiceMock);

                Assert.False(viewModel.ScoreboardOpenCommand.CanExecute());
            });
        }

        [Fact]
        public async Task ScoreboardOpenCommand_CanExecute_ReturnsTrue_WhenWebServiceIsActive()
        {
            await RunInSessionAsync(() =>
            {
                var webServiceMock = new Mock<IWebService>();
                webServiceMock.Setup(m => m.IsActive).Returns(true);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(webServiceMock: webServiceMock);

                Assert.True(viewModel.ScoreboardOpenCommand.CanExecute());
            });
        }

        [Fact]
        public async Task ScoreboardOpenCommand_Execute_CallsOpenServer()
        {
            await RunInSessionAsync(() =>
            {
                var webServiceMock = new Mock<IWebService>();
                webServiceMock.Setup(m => m.IsActive).Returns(true);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(webServiceMock: webServiceMock);

                viewModel.ScoreboardOpenCommand.Execute();

                webServiceMock.Verify(m => m.OpenServer(), Times.Once);
            });
        }

        [Fact]
        public async Task ScoreboardUpdateCommand_CanExecute_ReturnsFalse_WhenIsUpToDate()
        {
            await RunInSessionAsync(() =>
            {
                var scoreboardServiceMock = new Mock<IScoreboardService>();
                scoreboardServiceMock.Setup(m => m.IsUpToDate).Returns(true);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(scoreboardServiceMock: scoreboardServiceMock);

                Assert.False(viewModel.ScoreboardUpdateCommand.CanExecute());
            });
        }

        [Fact]
        public async Task ScoreboardUpdateCommand_CanExecute_ReturnsTrue_WhenNotUpToDate()
        {
            await RunInSessionAsync(() =>
            {
                var scoreboardServiceMock = new Mock<IScoreboardService>();
                scoreboardServiceMock.Setup(m => m.IsUpToDate).Returns(false);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(scoreboardServiceMock: scoreboardServiceMock);

                Assert.True(viewModel.ScoreboardUpdateCommand.CanExecute());
            });
        }

        [Fact]
        public async Task ScoreboardUpdateCommand_Execute_CallsUpdate()
        {
            await RunInSessionAsync(() =>
            {
                var scoreboardServiceMock = new Mock<IScoreboardService>();
                scoreboardServiceMock.Setup(m => m.IsUpToDate).Returns(false);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(scoreboardServiceMock: scoreboardServiceMock);

                viewModel.ScoreboardUpdateCommand.Execute();

                scoreboardServiceMock.Verify(m => m.Update(), Times.Once);
            });
        }

        #endregion Public Methods
    }
}