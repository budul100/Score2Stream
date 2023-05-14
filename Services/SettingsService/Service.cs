using Score2Stream.Core.Constants;
using Score2Stream.Core.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Score2Stream.SettingsService
{
    public class Service<T>
        : ISettingsService<T>, IDisposable
        where T : class
    {
        #region Private Fields

        private const string AppName = nameof(Score2Stream);

        private string filePath;
        private string folderPath;
        private bool isDisposed;
        private T settings;

        #endregion Private Fields

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public T Get()
        {
            if (settings == default)
            {
                Initialize();
            }

            return settings;
        }

        public void Initialize(string fileName = Constants.SettingsFileNameDefault)
        {
            this.folderPath = GetSettingsFolderPath();
            this.filePath = Path.Combine(
                path1: folderPath,
                path2: fileName);

            LoadSettings(filePath);

            if (settings is default(T))
            {
                settings = Activator.CreateInstance<T>();
            }
        }

        public void Save()
        {
            Task.Run(() => SaveAsync());
        }

        public async void SaveAsync()
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using var settingsFileStream = new FileStream(
                path: filePath,
                mode: FileMode.Create);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            await JsonSerializer.SerializeAsync(
                utf8Json: settingsFileStream,
                value: settings,
                options: options);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Task.Run(() => SaveAsync()).Wait();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static string GetSettingsFolderPath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var result = Path.Combine(
                path1: appDataFolder,
                path2: AppName);

            return result;
        }

        private void LoadSettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Task.Run(() => SaveAsync()).Wait();
            }

            using var settingsFileStream = new FileStream(
                path: filePath,
                mode: FileMode.Open,
                access: FileAccess.Read);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            try
            {
                settings = JsonSerializer.Deserialize<T>(
                    utf8Json: settingsFileStream,
                    options: options);
            }
            catch { }
        }

        #endregion Private Methods
    }
}