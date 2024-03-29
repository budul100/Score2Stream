﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ISettingsService<Session> settingsService;

        private CancellationTokenSource cancellationTokenSource;
        private WebServer webServer;
        private Task webServerTask;
        private WebSocket webSocket;
        private Task webSocketTask;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDispatcherService dispatcherService,
            IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.dispatcherService = dispatcherService;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ScoreboardUpdatedEvent>().Subscribe(
                action: m => OnScoreboardUpdate(m),
                keepSubscriberReferenceAlive: true);

            Task.Run(() => StartAsync());
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

            var urlWebSocket = $"http://{ipAddress}:{settingsService.Contents.Server.PortSocketHttp}";

            webSocket = new WebSocket(
                urlHttp: urlWebSocket,
                urlHttps: default);

            webSocketTask = Task.Run(
                function: async () => await dispatcherService.InvokeAsync(() => webSocket.RunAsync()),
                cancellationToken: cancellationTokenSource.Token);

            var urlWebServer = $"http://{ipAddress}:{settingsService.Contents.Server.PortServerHttp}";

            webServer = new WebServer(
                urlHttp: urlWebServer,
                urlHttps: default);

            webServerTask = Task.Run(
                function: async () => await dispatcherService.InvokeAsync(() => webServer.RunAsync()),
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

        private void OnScoreboardUpdate(string message)
        {
            if (IsActive)
            {
                webSocket.Set(
                    message: message,
                    requestDelay: settingsService.Contents.Server.WebSocketDelay);
            }
        }

        #endregion Private Methods
    }
}