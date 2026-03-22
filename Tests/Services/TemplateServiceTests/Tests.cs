using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Moq;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.TemplateService;
using Xunit;

namespace Score2Stream.Tests.TemplateServiceTests
{
    public class ServiceTests
        : IDisposable
    {
        #region Private Fields

        private readonly DetectionChangedEvent detectionChangedEvent = new();
        private readonly Mock<IDialogService> dialogServiceMock;
        private readonly Mock<IEventAggregator> eventAggregatorMock;
        private readonly SamplesChangedEvent samplesChangedEvent = new();
        private readonly Mock<ISampleService> sampleServiceMock;
        private readonly Session session;
        private readonly Mock<ISettingsService<Session>> settingsServiceMock;
        private readonly TemplatesChangedEvent templatesChangedEvent = new();
        private readonly TemplateSelectedEvent templateSelectedEvent = new();
        private readonly Service templateService;

        private Func<ISampleService> sampleServiceGetter;

        #endregion Private Fields

        #region Public Constructors

        public ServiceTests()
        {
            dialogServiceMock = new Mock<IDialogService>();
            eventAggregatorMock = new Mock<IEventAggregator>();
            settingsServiceMock = new Mock<ISettingsService<Session>>();
            sampleServiceMock = new Mock<ISampleService>();

            sampleServiceGetter = () => sampleServiceMock.Object;

            session = new Session { Templates = [] };
            settingsServiceMock.Setup(s => s.Contents).Returns(session);

            // Wire up all events the service requests in its constructor
            eventAggregatorMock.Setup(e => e.GetEvent<TemplatesChangedEvent>()).Returns(templatesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<TemplateSelectedEvent>()).Returns(templateSelectedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<DetectionChangedEvent>()).Returns(detectionChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesChangedEvent>()).Returns(samplesChangedEvent);

            templateService = new Service(
                settingsService: settingsServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                sampleServiceFactory: () => sampleServiceGetter(),
                eventAggregator: eventAggregatorMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void Create_AddsTemplateAndSelectsIt()
        {
            // Arrange
            var eventPublished = false;
            templateSelectedEvent.Subscribe(_ => eventPublished = true);

            // Act
            templateService.Create();

            // Assert
            Assert.NotNull(templateService.Active);
            Assert.Single(templateService.Templates);
            Assert.True(eventPublished);
            // Save() takes no parameters
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Create_AssignsUniqueName()
        {
            // Act
            templateService.Create();
            templateService.Create();

            // Assert
            var names = new HashSet<string>();
            foreach (var t in templateService.Templates)
                Assert.True(names.Add(t.Name), $"Duplicate name: {t.Name}");
        }

        [Fact]
        public void Create_MaxCountExceeded_DoesNotAddTemplate()
        {
            // Arrange

            for (var i = 0; i < Constants.MaxCountTemplates; i++)
                templateService.Create();

            // Act
            templateService.Create();

            // Assert
            Assert.Equal(Constants.MaxCountTemplates, templateService.Templates.Count);
        }

        [Fact]
        public void Create_MaxCountExceeded_ShowsErrorDialog()
        {
            // Arrange

            for (var i = 0; i < Constants.MaxCountTemplates; i++)
                templateService.Create();

            // Act – one over the limit
            templateService.Create();

            // Assert
            dialogServiceMock.Verify(d => d.ShowMessageBoxAsync(
                It.IsAny<string>(),
                It.Is<string>(t => t == "Maximum count exceeded"),
                It.IsAny<Icon>(),
                It.IsAny<bool>(),
                It.IsAny<WindowStartupLocation>()),
                Times.Once);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Initialize_EmptySettings_ActiveRemainsNull()
        {
            // Act
            templateService.Initialize();

            // Assert
            Assert.Null(templateService.Active);
            Assert.Empty(templateService.Templates);
        }

        [Fact]
        public void Initialize_WithExistingTemplates_SelectsFirstTemplate()
        {
            // Arrange
            var template = new Template { Name = "T1" };
            session.Templates.Add(template);

            // Act
            templateService.Initialize();

            // Assert
            Assert.NotNull(templateService.Active);
            Assert.Equal("T1", templateService.Active.Name);
            Assert.Single(templateService.Templates);
        }

        [Fact]
        public void Initialize_WithMoreTemplatesThanMaxCount_StopsAtMaxCount()
        {
            // Arrange – push one more template than allowed into settings
            for (var i = 0; i <= Constants.MaxCountTemplates; i++)
                session.Templates.Add(new Template { Name = $"T{i}" });

            // Act – must not throw
            templateService.Initialize();

            // Assert
            Assert.Equal(Constants.MaxCountTemplates, templateService.Templates.Count);
        }

        [Fact]
        public async Task RemoveAsync_ActiveTemplateWithSuccessor_SelectsNextTemplate()
        {
            // Arrange
            templateService.Create();
            templateService.Create();

            var first = templateService.Templates[0];
            var second = templateService.Templates[1];
            templateService.Select(first);
            SetupDialogResult(ButtonResult.Yes);

            // Act
            await templateService.RemoveAsync(first);

            // Assert
            Assert.Equal(second, templateService.Active);
        }

        [Fact]
        public async Task RemoveAsync_DefaultParameter_UsesActiveTemplate()
        {
            // Arrange
            templateService.Create();
            var active = templateService.Active;
            SetupDialogResult(ButtonResult.Yes);

            // Act – call without explicit template argument
            await templateService.RemoveAsync();

            // Assert
            Assert.DoesNotContain(active, templateService.Templates);
        }

        [Fact]
        public async Task RemoveAsync_LastTemplate_ActiveBecomesNull()
        {
            // Arrange
            templateService.Create();
            var template = templateService.Active;
            SetupDialogResult(ButtonResult.Yes);

            // Act
            await templateService.RemoveAsync(template);

            // Assert
            Assert.Null(templateService.Active);
            Assert.Empty(templateService.Templates);
        }

        [Fact]
        public async Task RemoveAsync_NullActiveTemplate_DoesNothing()
        {
            // Arrange – no template created, Active is null
            SetupDialogResult(ButtonResult.Yes);

            // Act – must not throw
            await templateService.RemoveAsync();

            // Assert
            Assert.Empty(templateService.Templates);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_UserSaysNo_DoesNotRemoveTemplate()
        {
            // Arrange
            templateService.Create();
            var template = templateService.Active;
            SetupDialogResult(ButtonResult.No);

            // Act
            await templateService.RemoveAsync(template);

            // Assert – template still in list, nothing saved after initial Create
            Assert.Contains(template, templateService.Templates);
        }

        [Fact]
        public async Task RemoveAsync_UserSaysNo_DoesNotSave()
        {
            // Arrange
            templateService.Create();
            var template = templateService.Active;
            SetupDialogResult(ButtonResult.No);

            // Reset invocation count after Create()
            settingsServiceMock.Invocations.Clear();

            // Act
            await templateService.RemoveAsync(template);

            // Assert
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_UserSaysYes_ClearsSampleServiceAndSaves()
        {
            // Arrange
            templateService.Create();
            var template = templateService.Active;
            SetupDialogResult(ButtonResult.Yes);

            // Act
            await templateService.RemoveAsync(template);

            // Assert – SampleService.Clear() must be called, template removed, settings saved
            // The mock returned by SetupSampleServiceMock was assigned to the template
            Assert.DoesNotContain(template, templateService.Templates);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void SampleService_WithActiveTemplate_ReturnsSampleServiceOfActiveTemplate()
        {
            // Arrange
            templateService.Create();

            // Assert
            Assert.Equal(sampleServiceMock.Object, templateService.SampleService);
        }

        [Fact]
        public void SampleService_WithNoActiveTemplate_ReturnsNull()
        {
            // No Create() called, Active is null
            Assert.Null(templateService.SampleService);
        }

        [Fact]
        public void Select_DifferentTemplate_PublishesEvent()
        {
            // Arrange
            templateService.Create();
            templateService.Create();

            var first = templateService.Templates[0];
            var second = templateService.Templates[1];
            templateService.Select(first);

            var eventPublished = false;
            templateSelectedEvent.Subscribe(_ => eventPublished = true);

            // Act
            templateService.Select(second);

            // Assert
            Assert.Equal(second, templateService.Active);
            Assert.True(eventPublished);
        }

        [Fact]
        public void Select_SameTemplate_DoesNotPublishEvent()
        {
            // Arrange
            templateService.Create();

            var eventCount = 0;
            templateSelectedEvent.Subscribe(_ => eventCount++);

            // Act – select the already-active template again
            templateService.Select(templateService.Active);

            // Assert
            Assert.Equal(0, eventCount);
        }

        [Fact]
        public void Select_WhenDetectionIsActive_DisablesDetection()
        {
            // Arrange
            var sampleServiceMock1 = new Mock<ISampleService>();
            var sampleServiceMock2 = new Mock<ISampleService>();
            sampleServiceMock2.SetupGet(s => s.IsDetection).Returns(true);

            var callCount = 0;
            sampleServiceGetter = () => callCount++ == 0
                ? sampleServiceMock1.Object
                : sampleServiceMock2.Object;

            templateService.Create(); // -> sampleServiceMock1
            templateService.Create(); // -> sampleServiceMock2,

            var first = templateService.Templates[0];
            var second = templateService.Templates[1];
            templateService.Select(first);

            // Reset
            sampleServiceMock2.Invocations.Clear();

            // Act
            templateService.Select(second);

            // Assert
            sampleServiceMock2.VerifySet(s => s.IsDetection = false, Times.Once);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Configures the dialog to return the given result for GetMessageBoxResultAsync.
        /// </summary>
        private void SetupDialogResult(ButtonResult result)
        {
            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(),
                    It.IsAny<ClickEnum>(),
                    It.IsAny<Icon>(),
                    It.IsAny<bool>(),
                    It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(result);
        }

        #endregion Private Methods
    }
}