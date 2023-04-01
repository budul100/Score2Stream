using Core.Events;
using Core.Interfaces;
using Prism.Events;
using System;
using System.Net;
using System.Net.Sockets;
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
        private readonly IScoreboardService scoreboardService;
        private CancellationTokenSource cancellationTokenSource;

        private WebServer webServer;
        private Task webServerTask;
        private WebSocket webSocket;
        private Task webSocketTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(IScoreboardService scoreboardService, IDispatcherService dispatcherService,
            IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: _ => OnScoreboardUpdate(),
                keepSubscriberReferenceAlive: true);
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

        public async Task StartAsync(int portWebServer, int portWebSocket)
        {
            if ((webServerTask?.IsCompleted == false)
                || (webSocketTask?.IsCompleted == false))
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            var ipAddress = GetLocalIPAddress();

            var urlWebSocket = $"http://{ipAddress}:{portWebSocket}";

            webSocket = new WebSocket(
                urlHttp: urlWebSocket,
                urlHttps: default);

            webSocketTask = Task.Run(
                function: async () => dispatcherService.Invoke(() => webSocket.RunAsync()),
                cancellationToken: cancellationTokenSource.Token);

            var urlWebServer = $"http://{ipAddress}:{portWebServer}";

            webServer = new WebServer(
                urlHttp: urlWebServer,
                urlHttps: default);

            webServerTask = Task.Run(
                function: async () => dispatcherService.Invoke(() => webServer.RunAsync()),
                cancellationToken: cancellationTokenSource.Token);

            eventAggregator
                .GetEvent<GraphicsUpdatedEvent>()
                .Publish();

            if (webServerTask.IsFaulted)
            {
                // To let the exceptions exit
                await webServerTask;
            }

            if (webSocketTask.IsFaulted)
            {
                // To let the exceptions exit
                await webSocketTask;
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

            if (webServerTask != default)
            {
                // To let the exceptions exit
                await webServerTask;
            }

            if (webSocketTask != default)
            {
                // To let the exceptions exit
                await webSocketTask;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void OnScoreboardUpdate()
        {
            if (IsActive)
            {
                webSocket.Set(scoreboardService.Message);
            }
        }

        #endregion Private Methods
    }
}