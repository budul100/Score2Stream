using Score2Stream.Commons.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Score2Stream.SettingsService
{
    public class Service<T>
        : ISettingsService<T>
        where T : class
    {
        #region Private Fields

        private const int WaitingPositions = 2;

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

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            try
            {
                Contents = JsonSerializer.Deserialize<T>(
                    utf8Json: settingsFileStream,
                    options: options);
            }
            catch { }
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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            try
            {
                JsonSerializer.Serialize(
                    utf8Json: settingsFileStream,
                    value: Contents,
                    options: options);
            }
            catch
            { }
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