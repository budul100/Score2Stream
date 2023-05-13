namespace Score2Stream.Core.Interfaces
{
    public interface ISettingsService<T> where T : class
    {
        #region Public Methods

        T Get();

        void Initialize(string fileName = "userSettings.json");

        void Save();

        #endregion Public Methods
    }
}