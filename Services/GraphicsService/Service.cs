using Core.Events;
using Core.Interfaces;
using Prism.Events;
using System.Threading;
using System.Threading.Tasks;
using WebserverService;

namespace GraphicsService
{
    public class Service
        : IGraphicsService
    {
        #region Private Fields

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private CancellationTokenSource cancellationTokenSource;

        private Task graphicsTask;
        private WebServer webServer;
        private WebSocket webSocket;

        #endregion Private Fields

        #region Public Constructors

        public Service(IDispatcherService dispatcherService, IEventAggregator eventAggregator)
        {
            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive => webSocket != default
            && webServer != default;

        #endregion Public Properties

        #region Public Methods

        public void Open(bool openHttps = false)
        {
            if (IsActive)
            {
                webServer.Open(openHttps);
            }
        }

        public void Set(string message)
        {
            if (IsActive)
            {
                webSocket.Set(message);
            }
        }

        public async Task StartAsync(string urlWebServer, string urlWebSocket)
        {
            if (graphicsTask?.IsCompleted == false)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            webServer = new WebServer(
                urlHttp: urlWebServer,
                urlHttps: default);

            var webServerTask = webServer.RunAsync();

            webSocket = new WebSocket(
                urlHttp: urlWebSocket,
                urlHttps: default);

            var webSocketTask = webSocket.RunAsync();

            graphicsTask = Task.Run(
                function: async () => dispatcherService.Invoke(() => Task.WhenAll(webServerTask, webSocketTask)),
                cancellationToken: cancellationTokenSource.Token);

            eventAggregator
                .GetEvent<GraphicsUpdatedEvent>()
                .Publish();

            if (graphicsTask.IsFaulted)
            {
                // To let the exceptions exit
                await graphicsTask;
            }
        }

        public async Task StopAsync()
        {
            if (cancellationTokenSource?.IsCancellationRequested == true)
            {
                return;
            }

            cancellationTokenSource?.Cancel();

            eventAggregator
                .GetEvent<GraphicsUpdatedEvent>()
                .Publish();

            if (graphicsTask != default)
            {
                await graphicsTask;
            }
        }

        #endregion Public Methods
    }
}