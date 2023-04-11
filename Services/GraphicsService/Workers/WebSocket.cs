using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphicsService.Workers
{
    internal class WebSocket
    {
        #region Private Fields

        private readonly WebApplication server;
        private string message;

        #endregion Private Fields

        #region Public Constructors

        public WebSocket(string urlHttp, string urlHttps, int requestDelay = 1000)
        {
            UrlHttp = urlHttp;
            UrlHttps = urlHttps;
            RequestDelay = requestDelay;

            var builder = WebApplication.CreateBuilder();

            var urls = new List<string>();
            if (urlHttps != default) urls.Add(urlHttps);
            if (urlHttp != default) urls.Add(urlHttp);

            builder.WebHost.UseUrls(urls.ToArray());

            server = builder.Build();
            server.UseWebSockets();

            server.Map(
                pattern: "",
                requestDelegate: SendRequest());
        }

        #endregion Public Constructors

        #region Public Properties

        public int RequestDelay { get; }

        public string UrlHttp { get; }

        public string UrlHttps { get; }

        #endregion Public Properties

        #region Public Methods

        public async Task RunAsync()
        {
            await server.RunAsync();
        }

        public void Set(string message)
        {
            this.message = message;
        }

        #endregion Public Methods

        #region Private Methods

        private RequestDelegate SendRequest()
        {
            return async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    while (true)
                    {
                        await webSocket.SendAsync(
                            buffer: Encoding.ASCII.GetBytes(message),
                            messageType: WebSocketMessageType.Text,
                            endOfMessage: true,
                            cancellationToken: CancellationToken.None);

                        await Task.Delay(RequestDelay);
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            };
        }

        #endregion Private Methods
    }
}