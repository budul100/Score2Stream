﻿using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Score2Stream.SettingsService
{
    public class Service<T>
        : ISettingsService<T>, IDisposable
        where T : class
    {
        #region Private Fields

        private const int WaitingPositions = 2;

        private readonly object saveLock = new();
        private readonly SemaphoreSlim waitLock = new(
            initialCount: 1,
            maxCount: WaitingPositions);

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

        private static string GetSettingsFolderPath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var result = Path.Combine(
                path1: appDataFolder,
                path2: Texts.AppName);

            return result;
        }

        private void LoadSettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                SaveSettings();
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

        private void SaveSettings()
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

            try
            {
                JsonSerializer.Serialize(
                    utf8Json: settingsFileStream,
                    value: settings,
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

        #endregion Private Methods
    }
}