﻿using Moq;
using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using WebcamModule.ViewModels;
using Xunit;

namespace WebcamModule.Tests
{
    public class WebcamViewModelFixture
    {
        #region Private Fields

        private readonly Mock<IClipService> clipServiceMock;
        private readonly Mock<IRegionManager> regionManagerMock;
        private readonly Mock<IWebcamService> webcamServiceMock;

        #endregion Private Fields

        //private const string MessageServiceDefaultMessage = "Some Value";
        //private Mock<IMessageService> _messageServiceMock;

        #region Public Constructors

        public WebcamViewModelFixture()
        {
            //var messageService = new Mock<IMessageService>();
            //messageService.Setup(x => x.GetMessage()).Returns(MessageServiceDefaultMessage);
            //_messageServiceMock = messageService;

            regionManagerMock = new Mock<IRegionManager>();
            webcamServiceMock = new Mock<IWebcamService>();
            clipServiceMock = new Mock<IClipService>();
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void MessageINotifyPropertyChangedCalled()
        {
            var viewModel = new WebcamViewModel(
                webcamServiceMock.Object,
                clipServiceMock.Object,
                regionManager: regionManagerMock.Object);

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