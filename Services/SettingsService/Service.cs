using Score2Stream.Core.Constants;
using Score2Stream.Core.Interfaces;
using System;
using System.IO;
using System.Text.Json;

namespace Score2Stream.SettingsService
{
    public class Service<T>
        : ISettingsService<T>
        where T : class
    {
        #region Private Fields

        private const string AppName = nameof(Score2Stream);

        private string filePath;
        private string folderPath;
        private T settings;

        #endregion Private Fields

        #region Public Methods

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

            JsonSerializer.Serialize(
                utf8Json: settingsFileStream,
                value: settings,
                options: options);
        }

        #endregion Public Methods

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
                Save();
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