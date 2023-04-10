using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IGraphicsService
    {
        #region Public Properties

        bool IsActive { get; }

        int PortServerHttp { get; set; }

        int PortServerHttps { get; set; }

        int PortSocketHttp { get; set; }

        int PortSocketHttps { get; set; }

        #endregion Public Properties

        #region Public Methods

        void Open(bool openHttps = false);

        Task ReloadAsync();

        Task StartAsync();

        Task StopAsync();

        #endregion Public Methods
    }
}