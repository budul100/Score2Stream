using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
{
    public interface IWebService
    {
        #region Public Properties

        bool IsActive { get; }

        #endregion Public Properties

        #region Public Methods

        void Open();

        Task ReloadAsync();

        Task StartAsync();

        Task StopAsync();

        #endregion Public Methods
    }
}