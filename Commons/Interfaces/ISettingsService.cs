namespace Score2Stream.Commons.Interfaces
{
    public interface ISettingsService<T>
        where T : class
    {
        #region Public Methods

        T Get();

        void Initialize(string fileName);

        void Save();

        #endregion Public Methods
    }
}