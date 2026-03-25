using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.WebService;
using Xunit;

namespace Score2Stream.Tests.WebServiceTests
{
    [Collection("WebService")]
    public class Tests
        : IAsyncDisposable
    {
        #region Private Fields

        // Use high port numbers unlikely to be in use during tests
        private const int TestServerPort = 19870;

        private const int TestSocketPort = 19871;
        private readonly Mock<IDispatcherService> dispatcherServiceMock;
        private readonly Mock<IEventAggregator> eventAggregatorMock;
        private readonly Mock<ScoreboardUpdatedEvent> scoreboardUpdatedEventMock;
        private readonly Mock<ServerStartedEvent> serverStartedEventMock;
        private readonly Mock<ISettingsService<Session>> settingsServiceMock;
        private readonly Service webService;
        private Mock<ServerStoppedEvent> serverStoppedEventMock;

        #endregion Private Fields

        #region Public Constructors

        public Tests()
        {
            dispatcherServiceMock = new Mock<IDispatcherService>();
            settingsServiceMock = new Mock<ISettingsService<Session>>();

            eventAggregatorMock = new Mock<IEventAggregator>();
            scoreboardUpdatedEventMock = new Mock<ScoreboardUpdatedEvent>();
            serverStartedEventMock = new Mock<ServerStartedEvent>();
            serverStoppedEventMock = new Mock<ServerStoppedEvent>();

            eventAggregatorMock
                .Setup(e => e.GetEvent<ScoreboardUpdatedEvent>())
                .Returns(scoreboardUpdatedEventMock.Object);

            eventAggregatorMock
                .Setup(e => e.GetEvent<ServerStartedEvent>())
                .Returns(serverStartedEventMock.Object);
            eventAggregatorMock
                .Setup(e => e.GetEvent<ServerStoppedEvent>())
                .Returns(serverStoppedEventMock.Object);

            dispatcherServiceMock
                .Setup(d => d.InvokeAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                .Returns((Func<Task> f, System.Threading.CancellationToken _) => Task.FromResult(f()));

            var session = new Session
            {
                Server = new Server
                {
                    PortServer = TestServerPort,
                    PortSocket = TestSocketPort,
                    DelaySocket = 100,
                }
            };

            settingsServiceMock
                .Setup(s => s.Contents)
                .Returns(session);

            webService = new Service(
                settingsService: settingsServiceMock.Object,
                dispatcherService: dispatcherServiceMock.Object,
                eventAggregator: eventAggregatorMock.Object);
        }

        #endregion Public Constructors

        #region Public Methods

        [Fact]
        public async Task Constructor_AfterStartup_IsActive()
        {
            // Give the fire-and-forget Task.Run(StartAsync) time to complete
            await Task.Delay(500);

            Assert.True(webService.IsActive);
        }

        public async ValueTask DisposeAsync()
        {
            await webService.StopAsync();
        }

        [Fact]
        public async Task ReloadAsync_DoesNotThrow()
        {
            await Task.Delay(500);

            var ex = await Record.ExceptionAsync(() => webService.ReloadAsync());

            Assert.Null(ex);
        }

        [Fact]
        public async Task ReloadAsync_RestoresIsActive()
        {
            await Task.Delay(500);

            await webService.ReloadAsync();
            await Task.Delay(300);

            Assert.True(webService.IsActive);
        }

        [Fact]
        public async Task ScoreboardUpdate_WhenActive_DoesNotThrow()
        {
            await Task.Delay(500);

            var ex = Record.Exception(()
                => scoreboardUpdatedEventMock.Object.Publish("{ score: 42 }"));

            Assert.Null(ex);
        }

        [Fact]
        public async Task ScoreboardUpdate_WhenNotActive_DoesNotThrow()
        {
            // Service not yet started (or stopped) — publishing should be silent
            await webService.StopAsync();

            // Simulate the event being raised
            var ex = Record.Exception(()
                => scoreboardUpdatedEventMock.Object.Publish("{ score: 1 }"));

            Assert.Null(ex);
        }

        [Fact]
        public async Task StartAsync_AfterStop_RestoresIsActive()
        {
            await Task.Delay(500);
            await webService.StopAsync();

            await webService.StartAsync();
            await Task.Delay(300);

            Assert.True(webService.IsActive);
        }

        [Fact]
        public async Task StartAsync_HttpServer_RespondsOnConfiguredPort()
        {
            await Task.Delay(500);

            // Resolve the same IP the service uses internally
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var localIp = host.AddressList
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString() ?? "127.0.0.1";

            using var client = new HttpClient();
            var response = await client.GetAsync(
                $"http://{localIp}:{TestServerPort}/",
                HttpCompletionOption.ResponseHeadersRead);

            Assert.True(
                response.StatusCode != HttpStatusCode.ServiceUnavailable,
                $"Expected server on {localIp}:{TestServerPort} to respond.");
        }

        [Fact]
        public async Task StartAsync_WhileAlreadyRunning_DoesNotStartSecondInstance()
        {
            await Task.Delay(500); // first startup complete

            // Second StartAsync should be a no-op
            var ex = await Record.ExceptionAsync(() => webService.StartAsync());

            Assert.Null(ex);
            Assert.True(webService.IsActive);
        }

        [Fact]
        public async Task StopAsync_CalledMultipleTimes_DoesNotThrow()
        {
            await Task.Delay(500);

            await webService.StopAsync();

            var ex = await Record.ExceptionAsync(() => webService.StopAsync());

            Assert.Null(ex);
        }

        [Fact]
        public async Task StopAsync_ThenStartAsync_Rapid_DoesNotThrow()
        {
            await Task.Delay(500);

            // Fire both in quick succession without awaiting in between
            var ex = await Record.ExceptionAsync(async () =>
            {
                var stop = webService.StopAsync();
                var start = webService.StartAsync();
                await Task.WhenAll(stop, start);
            });

            Assert.Null(ex);
        }

        [Fact]
        public async Task StopAsync_WhenNeverStartedManually_DoesNotThrow()
        {
            // Constructor fires StartAsync via Task.Run — stop immediately
            var ex = await Record.ExceptionAsync(() => webService.StopAsync());

            Assert.Null(ex);
        }

        [Fact]
        public async Task StopAsync_WhenRunning_SetsIsActiveToFalse()
        {
            await Task.Delay(500); // wait for startup

            await webService.StopAsync();

            Assert.False(webService.IsActive);
        }

        #endregion Public Methods
    }
}