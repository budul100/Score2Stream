using System;
using System.Threading.Tasks;
using Moq;
using OpenCvSharp;
using Prism.Regions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.MenuModule.ViewModels;
using Xunit;

namespace Score2Stream.Tests.MenuModuleTests
{
    [Collection("HeadlessUI")]
    public class SampleServiceTests
        : TestBase, IDisposable
    {
        #region Private Fields

        private readonly Mat mat;

        #endregion Private Fields

        #region Public Constructors

        public SampleServiceTests()
        {
            mat = new Mat(new Size(10, 10), MatType.CV_16SC1);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            mat?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task IsSampleDetection_SetFalse_DisablesDetection()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.IsSampleDetection = true;
                viewModel.IsSampleDetection = false;

                Assert.False(sampleService.IsDetection);
            });
        }

        [Fact]
        public async Task IsSampleDetection_SetTrue_WhenInputIsActive()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.IsSampleDetection = true;

                Assert.True(sampleService.IsDetection);
            });
        }

        [Fact]
        public async Task SampleAdd_ExceedsMaxCount_DoesNotThrow()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                var totalAttempts = Constants.MaxCountSamples + 10;

                for (var i = 0; i < totalAttempts; i++)
                {
                    viewModel.SampleAddCommand.Execute();
                }

                Assert.True(sampleService.Samples.Count <= Constants.MaxCountSamples,
                    $"Sample count {sampleService.Samples.Count} should not exceed {Constants.MaxCountSamples}.");
            });
        }

        [Fact]
        public async Task SampleAdd_MultipleSamples_IncrementsCount()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                var expectedCount = 5;

                for (var i = 0; i < expectedCount; i++)
                {
                    viewModel.SampleAddCommand.Execute();
                }

                Assert.Equal(expectedCount, sampleService.Samples.Count);
            });
        }

        [Fact]
        public async Task SampleAdd_WithValidSegment_AddsSample()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.SampleAddCommand.Execute();

                Assert.Single(sampleService.Samples);
            });
        }

        [Fact]
        public async Task SampleAddCommand_CanExecute_ReturnsFalseWhenSegmentIsNull()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, _) = CreateViewModelWithNullSegment();

                Assert.False(viewModel.SampleAddCommand.CanExecute());
            });
        }

        [Fact]
        public async Task SampleOrder_WithMultipleSamples_DoesNotThrow()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.SampleAddCommand.Execute();
                viewModel.SampleAddCommand.Execute();

                var exception = Record.Exception(
                    () => viewModel.SampleOrderCommand.Execute());

                Assert.Null(exception);
            });
        }

        [Fact]
        public async Task SampleOrderCommand_CanExecute_ReturnsFalseWhenNoSamples()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                Assert.False(viewModel.SampleOrderCommand.CanExecute());
            });
        }

        [Fact]
        public async Task SampleOrderCommand_CanExecute_ReturnsTrueWhenSamplesExist()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.SampleAddCommand.Execute();

                Assert.True(viewModel.SampleOrderCommand.CanExecute());
            });
        }

        [Fact]
        public async Task SampleRemove_WithSelectedSample_DecrementsCount()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.SampleAddCommand.Execute();
                viewModel.SampleAddCommand.Execute();

                viewModel.SampleRemoveCommand.Execute();

                Assert.Single(sampleService.Samples);
            });
        }

        [Fact]
        public async Task SampleRemove_WithSelectedSample_RemovesSample()
        {
            await RunInSessionAsync(() =>
            {
                var (viewModel, sampleService) = CreateViewModelWithService();

                viewModel.SampleAddCommand.Execute();
                var sample = sampleService.Active;

                viewModel.SampleRemoveCommand.Execute();

                Assert.DoesNotContain(sample, sampleService.Samples);
            });
        }

        #endregion Public Methods

        #region Private Methods

        private static (MenuViewModel ViewModel, ISampleService SampleService) CreateViewModelWithNullSegment()
        {
            var templateMock = new Mock<Template>();
            var eventAggregatorMock = CreateEventAggregatorMock();
            var session = new Session();

            var sessionSettingsServiceMock = new Mock<ISettingsService<Session>>();
            sessionSettingsServiceMock.Setup(m => m.Contents).Returns(session);

            var sampleService = new SampleService.Service(
                settingsService: sessionSettingsServiceMock.Object,
                recognitionService: new Mock<IRecognitionService>().Object,
                dialogService: new Mock<IDialogService>().Object,
                eventAggregator: eventAggregatorMock.Object);

            sampleService.Initialize(templateMock.Object);

            var templateServiceMock = new Mock<ITemplateService>();
            templateServiceMock.Setup(m => m.SampleService).Returns(sampleService);

            var inputServiceMock = new Mock<IInputService>();
            inputServiceMock.Setup(m => m.AreaService.Segment).Returns((Segment)null);

            var viewModel = new MenuViewModel(
                settingsService: sessionSettingsServiceMock.Object,
                webService: new Mock<IWebService>().Object,
                scoreboardService: new Mock<IScoreboardService>().Object,
                inputService: inputServiceMock.Object,
                templateService: templateServiceMock.Object,
                regionManager: new Mock<IRegionManager>().Object,
                dialogService: new Mock<IDialogService>().Object,
                eventAggregator: eventAggregatorMock.Object);

            return (viewModel, sampleService);
        }

        private (MenuViewModel ViewModel, ISampleService SampleService) CreateViewModelWithService()
        {
            var templateMock = new Mock<Template>();

            var area = new Area { Template = templateMock.Object };
            var segment = new Segment { Area = area, Image = mat };

            var eventAggregatorMock = CreateEventAggregatorMock();
            var session = new Session();

            var sessionSettingsServiceMock = new Mock<ISettingsService<Session>>();
            sessionSettingsServiceMock.Setup(m => m.Contents).Returns(session);

            var recognitionServiceMock = new Mock<IRecognitionService>();
            var dialogServiceMock = new Mock<IDialogService>();

            var sampleService = new SampleService.Service(
                settingsService: sessionSettingsServiceMock.Object,
                recognitionService: recognitionServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);

            sampleService.Initialize(templateMock.Object);

            var templateServiceMock = new Mock<ITemplateService>();
            templateServiceMock.Setup(m => m.SampleService).Returns(sampleService);

            var inputServiceMock = new Mock<IInputService>();
            inputServiceMock.Setup(m => m.AreaService.Segment).Returns(segment);
            inputServiceMock.Setup(m => m.IsActive).Returns(true);

            var viewModel = new MenuViewModel(
                settingsService: sessionSettingsServiceMock.Object,
                webService: new Mock<IWebService>().Object,
                scoreboardService: new Mock<IScoreboardService>().Object,
                inputService: inputServiceMock.Object,
                templateService: templateServiceMock.Object,
                regionManager: new Mock<IRegionManager>().Object,
                dialogService: dialogServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);

            return (viewModel, sampleService);
        }

        #endregion Private Methods
    }
}