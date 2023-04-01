using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IGraphicsService
    {
        #region Public Properties

        bool IsActive { get; }

        #endregion Public Properties

        #region Public Methods

        void Open(bool openHttps = false);

        void Set(string message);

        Task StartAsync(string urlWebServer, string urlWebSocket);

        Task StopAsync();

        #endregion Public Methods
    }
}