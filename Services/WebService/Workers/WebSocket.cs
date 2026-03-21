using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Score2Stream.WebService.Workers
{
    internal class WebSocket
    {
        #region Private Fields

        private readonly SemaphoreSlim channelsLock = new(1, 1);
        private readonly List<Channel<string>> clientChannels = [];
        private readonly WebApplication server;

        private CancellationToken cancellationToken;
        private string message;

        #endregion Private Fields

        #region Public Constructors

        public WebSocket(string urlHttp, string urlHttps)
        {
            UrlHttp = urlHttp;
            UrlHttps = urlHttps;

            var builder = WebApplication.CreateBuilder();

            var urls = new List<string>();

            if (urlHttps != default) urls.Add(urlHttps);
            if (urlHttp != default) urls.Add(urlHttp);

            var hostUrls = urls.ToArray();

            builder.WebHost.UseUrls(hostUrls);

            server = builder.Build();
            server.UseWebSockets();

            server.Map(
                pattern: "",
                requestDelegate: SendRequest());
        }

        #endregion Public Constructors

        #region Public Properties

        public string UrlHttp { get; }

        public string UrlHttps { get; }

        #endregion Public Properties

        #region Public Methods

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            this.cancellationToken = cancellationToken;

            await server.RunAsync(cancellationToken);
        }

        public void Set(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            this.message = message;

            var channels = clientChannels.ToArray();

            foreach (var channel in channels)
            {
                if (!channel.Writer.TryWrite(message))
                {
                    channel.Reader.TryRead(out _);
                    channel.Writer.TryWrite(message);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private RequestDelegate SendRequest()
        {
            return SendRequestAsyn;
        }

        private async Task SendRequestAsyn(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var channel = Channel.CreateBounded<string>(1);

                await channelsLock.WaitAsync();

                clientChannels.Add(channel);
                channelsLock.Release();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    channel.Writer.TryWrite(message);
                }

                var messages = channel.Reader
                    .ReadAllAsync(cancellationToken);

                await foreach (var message in messages)
                {
                    if (webSocket.State != WebSocketState.Open) break;

                    await webSocket.SendAsync(
                        buffer: Encoding.UTF8.GetBytes(message),
                        messageType: WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: cancellationToken);
                }

                await channelsLock.WaitAsync();

                clientChannels.Remove(channel);
                channelsLock.Release();
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        #endregion Private Methods
    }
}