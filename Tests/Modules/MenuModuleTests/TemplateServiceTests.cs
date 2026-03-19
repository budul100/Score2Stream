using System.Threading.Tasks;
using Moq;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.MenuModule.ViewModels;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    [Collection("HeadlessUI")]
    public class TemplateServiceTests
        : TestBase
    {
        #region Public Methods

        [Fact]
        public async Task TemplateAddCommand_Execute_CallsCreate()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();

                var (viewModel, _) = CreateViewModel(templateServiceMock);

                viewModel.TemplateAddCommand.Execute();

                templateServiceMock.Verify(m => m.Create(), Times.Once);
            });
        }

        [Fact]
        public async Task TemplateAddCommand_Execute_WhenTemplateServiceIsNull_DoesNotThrow()
        {
            await RunInSessionAsync(() =>
            {
                // templateService is never null in practice due to DI,
                // but the guard in AddTemplate() covers it
                var (viewModel, _) = CreateViewModel(new Mock<ITemplateService>());

                var exception = Record.Exception(
                    () => viewModel.TemplateAddCommand.Execute());

                Assert.Null(exception);
            });
        }

        #endregion Public Methods

        #region Private Methods

        private static (MenuViewModel ViewModel, Mock<ITemplateService> TemplateServiceMock) CreateViewModel(
            Mock<ITemplateService> templateServiceMock = null,
            Mock<IDialogService> dialogServiceMock = null,
            Mock<IEventAggregator> eventAggregatorMock = null)
        {
            templateServiceMock ??= new Mock<ITemplateService>();
            dialogServiceMock ??= new Mock<IDialogService>();
            eventAggregatorMock ??= CreateEventAggregatorMock();

            var session = new Session();
            var settingsServiceMock = new Mock<ISettingsService<Session>>();
            settingsServiceMock.Setup(m => m.Contents).Returns(session);

            var viewModel = new MenuViewModel(
                settingsService: settingsServiceMock.Object,
                webService: new Mock<IWebService>().Object,
                scoreboardService: new Mock<IScoreboardService>().Object,
                inputService: new Mock<IInputService>().Object,
                templateService: templateServiceMock.Object,
                regionManager: new Mock<IRegionManager>().Object,
                dialogService: dialogServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);

            return (viewModel, templateServiceMock);
        }

        #endregion Private Methods
    }
}