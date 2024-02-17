using System;
using System.IO;
using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Extensions
{
    public static class AppExtensions
    {
        #region Private Fields

        private static FileStream lockFile;

        #endregion Private Fields

        #region Public Methods

        public static string GetAppDataFolder()
        {
            var localAppFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var result = Path.Combine(
                path1: localAppFolder,
                path2: Texts.AppName);

            return result;
        }

        public static bool IsSingleInstance()
        {
            var folder = AppExtensions.GetAppDataFolder();

            var path = Path.Combine(folder, ".lock");

            try
            {
                Directory.CreateDirectory(folder);

                lockFile = File.Open(
                    path: path,
                    mode: FileMode.OpenOrCreate,
                    access: FileAccess.ReadWrite,
                    share: FileShare.None);

                lockFile.Lock(
                    position: 0,
                    length: 0);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Public Methods
    }
}