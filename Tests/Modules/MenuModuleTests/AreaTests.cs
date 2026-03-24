using System.Threading.Tasks;
using Moq;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Tests.MenuModuleTests.Base;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    [Collection("HeadlessUI")]
    public class AreaTests(HeadlessSessionFixture fixture)
        : TestBase(fixture)
    {
        #region Public Methods

        [Fact]
        public async Task AreaOrderAllCommand_CanExecute_ReturnsFalse_WhenNoAreas()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Areas).Returns([]);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                Assert.False(viewModel.AreaOrderAllCommand.CanExecute());
            });
        }

        [Fact]
        public async Task AreaOrderAllCommand_Execute_CallsOrder_WithTrue()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Areas).Returns([new Area()]);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                viewModel.AreaOrderAllCommand.Execute();

                areaServiceMock.Verify(a => a.Order(true), Times.Once);
            });
        }

        [Fact]
        public async Task AreaRemoveAllCommand_CanExecute_ReturnsFalse_WhenNoAreas()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Areas).Returns([]);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                Assert.False(viewModel.AreaRemoveAllCommand.CanExecute());
            });
        }

        [Fact]
        public async Task AreaRemoveAllCommand_CanExecute_ReturnsTrue_WhenAreasExist()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Areas).Returns([new Area()]);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                Assert.True(viewModel.AreaRemoveAllCommand.CanExecute());
            });
        }

        [Fact]
        public async Task AreaRemoveAllCommand_Execute_CallsClearAsync()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Areas).Returns([new Area()]);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                viewModel.AreaRemoveAllCommand.Execute();

                areaServiceMock.Verify(a => a.ClearAsync(), Times.Once);
            });
        }

        [Fact]
        public async Task AreaRemoveCommand_CanExecute_ReturnsFalse_WhenNoActiveArea()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Active).Returns((Area)null);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                Assert.False(viewModel.AreaRemoveCommand.CanExecute());
            });
        }

        [Fact]
        public async Task AreaRemoveCommand_CanExecute_ReturnsTrue_WhenAreaIsActive()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Active).Returns(new Area());

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                Assert.True(viewModel.AreaRemoveCommand.CanExecute());
            });
        }

        [Fact]
        public async Task AreaRemoveCommand_Execute_CallsRemoveAsync()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.Active).Returns(new Area());

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                viewModel.AreaRemoveCommand.Execute();

                areaServiceMock.Verify(a => a.RemoveAsync(), Times.Once);
            });
        }

        [Fact]
        public async Task AreaUndoCommand_CanExecute_ReturnsFalse_WhenCannotUndo()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.CanUndo).Returns(false);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                Assert.False(viewModel.AreaUndoCommand.CanExecute());
            });
        }

        [Fact]
        public async Task AreaUndoCommand_CanExecute_ReturnsTrue_WhenCanUndo()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.CanUndo).Returns(true);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                Assert.True(viewModel.AreaUndoCommand.CanExecute());
            });
        }

        [Fact]
        public async Task AreaUndoCommand_Execute_CallsUndo()
        {
            await RunInSessionAsync(() =>
            {
                var areaServiceMock = new Mock<IAreaService>();
                areaServiceMock.Setup(a => a.CanUndo).Returns(true);

                var inputServiceMock = new Mock<IInputService>();
                inputServiceMock.Setup(m => m.AreaService).Returns(areaServiceMock.Object);

                var (viewModel, _, _, _, _, _, _, _) = CreateViewModel(inputServiceMock: inputServiceMock);

                viewModel.AreaUndoCommand.Execute();

                areaServiceMock.Verify(a => a.Undo(), Times.Once);
            });
        }

        #endregion Public Methods
    }
}