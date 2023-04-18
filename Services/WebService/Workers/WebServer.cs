using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Score2Stream.WebService.Workers
{
    internal class WebServer
    {
        #region Private Fields

        private const string DirNameRoot = "wwwroot";

        private readonly WebApplication server;

        #endregion Private Fields

        #region Public Constructors

        public WebServer(string urlHttp, string urlHttps)
        {
            UrlHttp = urlHttp;
            UrlHttps = urlHttps;

            var urls = GetUrls().ToArray();
            var fileProvider = GetRootDirProvider();

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls(urls);

            server = builder.Build();
            server.UseHttpsRedirection();

            server.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
            });
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

        #region Private Methods

        private PhysicalFileProvider GetRootDirProvider()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            var rootPath = Path.Combine(
                path1: basePath,
                path2: DirNameRoot);

            if (!Directory.Exists(rootPath))
            {
                var assembly = typeof(WebServer).Assembly;
                Directory.CreateDirectory(rootPath);

                var resourceNames = assembly.GetManifestResourceNames()
                    .Where(n => n.Contains(DirNameRoot)).ToArray();

                foreach (var resourceName in resourceNames)
                {
                    var filePath = Path.Combine(
                        path1: basePath,
                        path2: resourceName);

                    var fileInfo = new FileInfo(filePath);
                    fileInfo.Directory.Create();

                    using var stream = File.Create(filePath);
                    using var resourceStream = assembly.GetManifestResourceStream(resourceName);
                    resourceStream.CopyTo(stream);
                }
            }

            var result = new PhysicalFileProvider(basePath);

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