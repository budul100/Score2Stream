using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Score2Stream.WebService.Workers
{
    internal class WebServer
    {
        #region Private Fields

        private const string BaseNamespace = "wwwroot";

        private readonly WebApplication server;

        #endregion Private Fields

        #region Public Constructors

        public WebServer(string urlHttp, string urlHttps)
        {
            UrlHttp = urlHttp;
            UrlHttps = urlHttps;

            var builder = WebApplication.CreateBuilder();

            var urls = new List<string>();
            if (urlHttps != default) urls.Add(urlHttps);
            if (urlHttp != default) urls.Add(urlHttp);

            builder.WebHost.UseUrls(urls.ToArray());

            server = builder.Build();

            var embeddedFileProvider = new EmbeddedFileProvider(
                assembly: typeof(WebServer).Assembly,
                baseNamespace: BaseNamespace);

            server.UseFileServer(new FileServerOptions
            {
                FileProvider = embeddedFileProvider,
                RequestPath = "",
                EnableDefaultFiles = true,
            });

            server.UseHttpsRedirection();
        }

        #endregion Public Constructors

        #region Public Properties

        public string UrlHttp { get; }

        public string UrlHttps { get; }

        #endregion Public Properties

        #region Public Methods

        public void Open(bool openHttps = false)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = openHttps ? UrlHttps : UrlHttp,
                UseShellExecute = true
            });
        }

        public async Task RunAsync()
        {
            await server.RunAsync();
        }

        #endregion Public Methods
    }
}