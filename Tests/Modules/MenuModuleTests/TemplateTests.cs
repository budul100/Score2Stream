using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Moq;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Tests.MenuModuleTests.Base;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    [Collection("HeadlessUI")]
    public class TemplateTests(HeadlessSessionFixture fixture)
        : TestBase(fixture)
    {
        #region Public Methods

        [Fact]
        public async Task IsActiveSample_ReturnsFalse_WhenTemplateActiveIsNull()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Active).Returns((Template)null);
                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(templateServiceMock: templateServiceMock);

                Assert.False(viewModel.IsActiveSample);
            });
        }

        [Fact]
        public async Task IsActiveSample_ReturnsTrue_WhenTemplateActiveIsSet()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Active).Returns(new Template());
                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(templateServiceMock: templateServiceMock);

                Assert.True(viewModel.IsActiveSample);
            });
        }

        [Fact]
        public async Task SampleAddCommand_CanExecute_ReturnsFalse_WhenActiveSegmentIsNull()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Active).Returns(new Template());

                var inputServiceMock = new Mock<IInputService>();
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.ActiveSegment).Returns((Segment)null);
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(
                    inputServiceMock: inputServiceMock,
                    templateServiceMock: templateServiceMock);

                Assert.False(viewModel.SampleAddCommand.CanExecute());
            });
        }

        [Fact]
        public async Task SampleAddCommand_CanExecute_ReturnsFalse_WhenTemplateActiveIsNull()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Active).Returns((Template)null);

                var inputServiceMock = new Mock<IInputService>();
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.ActiveSegment).Returns(new Segment());
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(
                    inputServiceMock: inputServiceMock,
                    templateServiceMock: templateServiceMock);

                Assert.False(viewModel.SampleAddCommand.CanExecute());
            });
        }

        [Fact]
        public async Task TemplateAddCommand_CanExecute_ReturnsTrue_WhenBelowMaxCount()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Templates).Returns(new ObservableCollection<Template>());
                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(templateServiceMock: templateServiceMock);

                Assert.True(viewModel.TemplateAddCommand.CanExecute());
            });
        }

        [Fact]
        public async Task TemplateAddCommand_Execute_CallsCreate()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Templates).Returns(new ObservableCollection<Template>());
                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(templateServiceMock: templateServiceMock);

                viewModel.TemplateAddCommand.Execute();

                templateServiceMock.Verify(m => m.Create(), Times.Once);
            });
        }

        [Fact]
        public async Task TemplateAddCommand_Execute_CallsCreate_ExactlyOnce_PerInvocation()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Templates).Returns(new ObservableCollection<Template>());
                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(templateServiceMock: templateServiceMock);

                viewModel.TemplateAddCommand.Execute();
                viewModel.TemplateAddCommand.Execute();
                viewModel.TemplateAddCommand.Execute();

                templateServiceMock.Verify(m => m.Create(), Times.Exactly(3));
            });
        }

        [Fact]
        public async Task TemplateAddCommand_Execute_DoesNotThrow()
        {
            await RunInSessionAsync(() =>
            {
                var templateServiceMock = new Mock<ITemplateService>();
                templateServiceMock.Setup(m => m.Templates).Returns(new ObservableCollection<Template>());
                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(templateServiceMock: templateServiceMock);

                var exception = Record.Exception(() => viewModel.TemplateAddCommand.Execute());

                Assert.Null(exception);
            });
        }

        #endregion Public Methods
    }
}