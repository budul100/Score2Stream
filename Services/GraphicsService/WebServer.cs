using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WebserverService
{
    internal class WebServer
    {
        #region Private Fields

        private readonly WebApplication server;

        #endregion Private Fields

        #region Public Constructors

        public WebServer(string urlHttp, string urlHttps)
        {
            var builder = WebApplication.CreateBuilder();

            var urls = new List<string>();
            if (urlHttps != default) urls.Add(urlHttps);
            if (urlHttp != default) urls.Add(urlHttp);

            builder.WebHost.UseUrls(urls.ToArray());

            server = builder.Build();

            server.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
                RequestPath = "",
                EnableDefaultFiles = true,
            });

            server.UseHttpsRedirection();
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task RunAsync()
        {
            await server.RunAsync();
        }

        #endregion Public Methods
    }
}