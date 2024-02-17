using System;

namespace Score2Stream.Commons.Interfaces
{
    public interface ISettingsService<T>
        : IDisposable
        where T : class
    {
        #region Public Properties

        T Contents { get; }

        string Path { get; }

        #endregion Public Properties

        #region Public Methods

        string GetPath(string appName, string fileName,
            Environment.SpecialFolder baseFolder = Environment.SpecialFolder.LocalApplicationData);

        void Load(string filePath);

        void Save(string filePath = default);

        #endregion Public Methods
    }
}