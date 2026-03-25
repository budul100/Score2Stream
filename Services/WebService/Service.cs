using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Graphics;
using Score2Stream.Commons.Events.Scoreboard;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;
using Score2Stream.WebService.Workers;

namespace Score2Stream.WebService
{
    public class Service
        : IWebService
    {
        #region Private Fields

        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogger logger;
        private readonly ISettingsService<Session> settingsService;

        private CancellationTokenSource cancellationTokenSource;
        private WebServer webServer;
        private Task webServerTask;
        private WebSocket webSocket;
        private Task webSocketTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDispatcherService dispatcherService,
            IEventAggregator eventAggregator, ILogger<Service> logger = default)
        {
            this.settingsService = settingsService;
            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;
            this.logger = logger;

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: OnScoreboardUpdate,
                keepSubscriberReferenceAlive: true);

            _ = StartAsync().ContinueWith(
                continuationAction: OnFailure,
                continuationOptions: TaskContinuationOptions.OnlyOnFaulted);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive => webSocket != default
            && webServer != default;

        #endregion Public Properties

        #region Public Methods

        public void OpenRoot()
        {
            webServer.OpenRoot();
        }

        public void OpenServer()
        {
            if (IsActive)
            {
                webServer.OpenServer();
            }
        }

        public async Task ReloadAsync()
        {
            await StopAsync();

            await StartAsync();
        }

        public async Task StartAsync()
        {
            if ((webServerTask?.IsCompleted == false)
                || (webSocketTask?.IsCompleted == false))
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            var ipAddress = GetLocalIPAddress();

            var urlWebSocket = $"http://{ipAddress}:{settingsService.Contents.Server.PortSocket}";

            webSocket = new WebSocket(
                urlHttp: urlWebSocket,
                urlHttps: default);

            webSocketTask = Task.Run(
                function: async () => await dispatcherService.InvokeAsync(
                    function: () => webSocket.RunAsync(cancellationTokenSource.Token),
                    cancellationToken: cancellationTokenSource.Token),
                cancellationToken: cancellationTokenSource.Token);

            var urlWebServer = $"http://{ipAddress}:{settingsService.Contents.Server.PortServer}";

            webServer = new WebServer(
                url: urlWebServer,
                socketPort: settingsService.Contents.Server.PortSocket,
                updateInterval: settingsService.Contents.Server.DelaySocket);

            webServerTask = Task.Run(
                function: async () => await dispatcherService.InvokeAsync(
                    function: () => webServer.RunAsync(cancellationTokenSource.Token),
                    cancellationToken: cancellationTokenSource.Token),
                cancellationToken: cancellationTokenSource.Token);

            eventAggregator.GetEvent<ServerStartedEvent>().Publish();

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

            eventAggregator.GetEvent<ServerStoppedEvent>().Publish();

            var timeout = TimeSpan.FromSeconds(Constants.ShutdownTimeoutSecs);

            if (webServerTask != default)
            {
                try
                {
                    await webServerTask.WaitAsync(timeout);
                }
                catch { }
            }

            if (webSocketTask != default)
            {
                try
                {
                    await webSocketTask.WaitAsync(timeout);
                }
                catch { }
            }

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = default;

            webServer = default;
            webSocket = default;
        }

        #endregion Public Methods

        #region Private Methods

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());

                var ip = host.AddressList
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

                if (ip != default)
                {
                    return ip.ToString();
                }

                logger?.LogWarning(
                    "No IPv4 network adapter found. Falling back to localhost.");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(
                    exception: ex,
                    message: "Failed to resolve local IP address. Falling back to localhost.");
            }

            return IPAddress.Loopback.ToString(); // "127.0.0.1"
        }

        private void OnFailure(Task task)
        {
            logger?.LogError(
                exception: task.Exception,
                message: "WebService failed to start.");
        }

        private void OnScoreboardUpdate(string message)
        {
            if (IsActive)
            {
                webSocket.Set(message);
            }
        }

        #endregion Private Methods
    }
}