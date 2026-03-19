using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Moq;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
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

        private readonly Mock<IContainerProvider> containerProviderMock;
        private readonly Mock<IDialogService> dialogServiceMock;
        private readonly Mock<IEventAggregator> eventAggregatorMock;
        private readonly SampleModifiedEvent sampleModifiedEvent = new();
        private readonly SamplesChangedEvent samplesChangedEvent = new();
        private readonly SamplesOrderedEvent samplesOrderedEvent = new();
        private readonly Session session;
        private readonly Mock<ISettingsService<Session>> settingsServiceMock;
        private readonly TemplatesChangedEvent templatesChangedEvent = new();
        private readonly TemplateSelectedEvent templateSelectedEvent = new();
        private readonly Service templateService;

        #endregion Private Fields

        #region Public Constructors

        public ServiceTests()
        {
            containerProviderMock = new Mock<IContainerProvider>();
            dialogServiceMock = new Mock<IDialogService>();
            eventAggregatorMock = new Mock<IEventAggregator>();
            settingsServiceMock = new Mock<ISettingsService<Session>>();

            session = new Session { Templates = [] };
            settingsServiceMock.Setup(s => s.Contents).Returns(session);

            eventAggregatorMock.Setup(e => e.GetEvent<TemplatesChangedEvent>()).Returns(templatesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<TemplateSelectedEvent>()).Returns(templateSelectedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesChangedEvent>()).Returns(samplesChangedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SamplesOrderedEvent>()).Returns(samplesOrderedEvent);
            eventAggregatorMock.Setup(e => e.GetEvent<SampleModifiedEvent>()).Returns(sampleModifiedEvent);

            templateService = new Service(
                settingsService: settingsServiceMock.Object,
                dialogService: dialogServiceMock.Object,
                containerProvider: containerProviderMock.Object,
                eventAggregator: eventAggregatorMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public void Create_AddsTemplateAndSelectsIt()
        {
            // Arrange
            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

            var eventPublished = false;
            templateSelectedEvent.Subscribe(_ => eventPublished = true);

            // Act
            templateService.Create();

            // Assert
            Assert.NotNull(templateService.Active);
            Assert.Single(templateService.Templates);
            Assert.True(eventPublished);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Create_MaxCountExceeded_ShowsErrorDialog()
        {
            // Arrange
            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

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
        public void EventAggregator_SamplesChanged_DoesNotSaveWithoutActiveTemplate()
        {
            // Act
            samplesChangedEvent.Publish();

            // Assert
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
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

            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

            // Act
            templateService.Initialize();

            // Assert
            Assert.NotNull(templateService.Active);
            Assert.Equal("T1", templateService.Active.Name);
            Assert.Single(templateService.Templates);
        }

        [Fact]
        public async Task RemoveAsync_ActiveTemplateWithSuccessor_SelectsNextTemplate()
        {
            // Arrange
            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

            templateService.Create();
            templateService.Create();

            var first = templateService.Templates[0];
            var second = templateService.Templates[1];

            templateService.Select(first);

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.Yes);

            // Act
            await templateService.RemoveAsync(first);

            // Assert
            Assert.Equal(second, templateService.Active);
        }

        [Fact]
        public async Task RemoveAsync_LastTemplate_ActiveBecomesNull()
        {
            // Arrange
            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

            templateService.Create();

            var template = templateService.Active;

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.Yes);

            // Act
            await templateService.RemoveAsync(template);

            // Assert
            Assert.Null(templateService.Active);
            Assert.Empty(templateService.Templates);
        }

        [Fact]
        public async Task RemoveAsync_UserSaysNo_DoesNotClearSampleService()
        {
            // Arrange
            var sampleServiceMock = new Mock<ISampleService>();
            var template = new Template
            {
                Name = "T1",
                SampleService = sampleServiceMock.Object,
            };

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.No);

            // Act
            await templateService.RemoveAsync(template);

            // Assert
            sampleServiceMock.Verify(s => s.Clear(), Times.Never);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_UserSaysYes_ClearsSampleServiceAndSaves()
        {
            // Arrange
            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

            templateService.Create();

            var template = templateService.Active;
            session.Templates = [template];

            dialogServiceMock
                .Setup(d => d.GetMessageBoxResultAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ButtonEnum>(),
                    It.IsAny<ClickEnum>(), It.IsAny<ClickEnum>(), It.IsAny<Icon>(),
                    It.IsAny<bool>(), It.IsAny<WindowStartupLocation>()))
                .ReturnsAsync(ButtonResult.Yes);

            // Act
            await templateService.RemoveAsync(template);

            // Assert
            sampleServiceMock.Verify(s => s.Clear(), Times.Once);
            Assert.DoesNotContain(template, templateService.Templates);
            settingsServiceMock.Verify(s => s.Save(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Select_DifferentTemplate_PublishesEvent()
        {
            // Arrange
            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

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
            var sampleServiceMock = new Mock<ISampleService>();
            containerProviderMock
                .Setup(c => c.Resolve(typeof(ISampleService)))
                .Returns(sampleServiceMock.Object);

            templateService.Create();

            var eventCount = 0;
            templateSelectedEvent.Subscribe(_ => eventCount++);

            // Act – select same template again
            templateService.Select(templateService.Active);

            // Assert
            Assert.Equal(0, eventCount);
        }

        #endregion Public Methods
    }
}