using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.SettingsService
{
    public class Service<T>(ILogger<Service<T>> logger = default)
        : ISettingsService<T>
        where T : class
    {
        #region Private Fields

        private const int WaitingPositions = 2;

        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private static readonly JsonSerializerOptions SerializeOptions = new()
        {
            WriteIndented = true
        };

        private readonly object saveLock = new();

        private readonly SemaphoreSlim waitLock = new(
            initialCount: 1,
            maxCount: WaitingPositions);

        private bool isDisposed;

        #endregion Private Fields

        #region Public Properties

        public T Contents { get; private set; }

        public string Path { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public string GetPath(string appName, string fileName,
            Environment.SpecialFolder baseFolder = Environment.SpecialFolder.LocalApplicationData)
        {
            var appDataFolder = Environment.GetFolderPath(baseFolder);

            var result = System.IO.Path.Combine(
                path1: appDataFolder,
                path2: appName,
                path3: fileName);

            return result;
        }

        public void Load(string filePath)
        {
            SetPath(filePath);

            LoadSettings();

            if (Contents is default(T))
            {
                Contents = Activator.CreateInstance<T>();
            }
        }

        public void Save(string filePath = default)
        {
            SetPath(filePath);

            Task.Run(() => SaveSettingsAsync());
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    SaveSettings();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void LoadSettings()
        {
            if (!File.Exists(Path))
            {
                SaveSettings();
            }

            using var settingsFileStream = new FileStream(
                path: Path,
                mode: FileMode.Open,
                access: FileAccess.Read);

            try
            {
                Contents = JsonSerializer.Deserialize<T>(
                    utf8Json: settingsFileStream,
                    options: DeserializeOptions);
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    ex, 
                    "Loading of settings failed for path '{Path}'.", 
                    Path);
            }
        }

        private void SaveSettings()
        {
            var folderPath = System.IO.Path.GetDirectoryName(Path);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using var settingsFileStream = new FileStream(
                path: Path,
                mode: FileMode.Create);

            try
            {
                JsonSerializer.Serialize(
                    utf8Json: settingsFileStream,
                    value: Contents,
                    options: SerializeOptions);
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    ex, 
                    "Saving of settings failed for path '{Path}'.", 
                    Path);
            }
        }

        private async Task SaveSettingsAsync()
        {
            if (waitLock.CurrentCount < WaitingPositions)
            {
                await waitLock.WaitAsync();

                try
                {
                    lock (saveLock)
                    {
                        SaveSettings();
                    }
                }
                finally
                {
                    waitLock.Release();
                }
            }
        }

        private void SetPath(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                Path = System.IO.Path.GetFullPath(filePath);
            }
        }

        #endregion Private Methods
    }
}