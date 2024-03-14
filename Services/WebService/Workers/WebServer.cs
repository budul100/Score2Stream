using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Score2Stream.WebService.Workers
{
    internal class WebServer
    {
        #region Private Fields

        private const string RootDirectory = "wwwroot";

        private readonly WebApplication server;

        #endregion Private Fields

        #region Public Constructors

        public WebServer(string urlHttp, string urlHttps)
        {
            UrlHttp = urlHttp;
            UrlHttps = urlHttps;

            var urls = GetUrls().ToArray();

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls(urls);

            var fileProvider = GetFileProvider(builder.Environment.ContentRootPath);

            var fileServerOptions = new FileServerOptions()
            {
                EnableDefaultFiles = true,
                EnableDirectoryBrowsing = false,
                FileProvider = fileProvider,
            };

            server = builder.Build();

            server.UseHttpsRedirection();
            server.UseFileServer(fileServerOptions);
        }

        #endregion Public Constructors

        #region Public Properties

        public string UrlHttp { get; }

        public string UrlHttps { get; }

        #endregion Public Properties

        #region Public Methods

        public void Open(bool openHttps = false)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = openHttps ? UrlHttps : UrlHttp,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        public async Task RunAsync()
        {
            await server.RunAsync();
        }

        #endregion Public Methods

        #region Private Methods

        private static PhysicalFileProvider GetFileProvider(string contentRootPath)
        {
            var rootPath = Path.Combine(
                path1: contentRootPath,
                path2: RootDirectory);

            if (!Directory.Exists(rootPath))
            {
                var assembly = typeof(WebServer).Assembly;
                Directory.CreateDirectory(rootPath);

                var resourceNames = assembly.GetManifestResourceNames()
                    .Where(n => n.Contains(RootDirectory)).ToArray();

                foreach (var resourceName in resourceNames)
                {
                    var filePath = Path.Combine(
                        path1: contentRootPath,
                        path2: resourceName);

                    var fileInfo = new FileInfo(filePath);
                    fileInfo.Directory.Create();

                    using var stream = File.Create(filePath);
                    using var resourceStream = assembly.GetManifestResourceStream(resourceName);
                    resourceStream.CopyTo(stream);
                }
            }

            var result = new PhysicalFileProvider(rootPath);

            return result;
        }

        private IEnumerable<string> GetUrls()
        {
            if (!string.IsNullOrWhiteSpace(UrlHttp))
            {
                yield return UrlHttp.Trim();
            }

            if (!string.IsNullOrWhiteSpace(UrlHttps))
            {
                yield return UrlHttps.Trim();
            }
        }

        #endregion Private Methods
    }
}