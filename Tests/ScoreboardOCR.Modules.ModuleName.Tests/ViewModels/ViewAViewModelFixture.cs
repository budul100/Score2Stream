using Xunit;

namespace ScoreboardOCR.Modules.ModuleName.Tests.ViewModels
{
    public class ViewAViewModelFixture
    {
        //private const string MessageServiceDefaultMessage = "Some Value";
        //private Mock<IMessageService> _messageServiceMock;
        //private Mock<IRegionManager> _regionManagerMock;

        #region Public Constructors

        public ViewAViewModelFixture()
        {
            //var messageService = new Mock<IMessageService>();
            //messageService.Setup(x => x.GetMessage()).Returns(MessageServiceDefaultMessage);
            //_messageServiceMock = messageService;

            //_regionManagerMock = new Mock<IRegionManager>();
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void MessageINotifyPropertyChangedCalled()
        {
            //var vm = new WebcamViewModel(_regionManagerMock.Object, _messageServiceMock.Object);
            //Assert.PropertyChanged(vm, nameof(vm.Message), () => vm.Message = "Changed");
        }

        [Fact]
        public void MessagePropertyValueUpdated()
        {
            //var vm = new WebcamViewModel(_regionManagerMock.Object, _messageServiceMock.Object);

            //_messageServiceMock.Verify(x => x.GetMessage(), Times.Once);

            //Assert.Equal(MessageServiceDefaultMessage, vm.Message);
        }

        #endregion Public Methods
    }
}