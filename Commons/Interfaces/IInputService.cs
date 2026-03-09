using System.Collections.Generic;
using System.Threading.Tasks;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IInputService
    {
        #region Public Properties

        Input Active { get; }

        IAreaService AreaService { get; }

        IReadOnlyList<Input> Inputs { get; }

        bool IsActive { get; }

        float Rotation { get; set; }

        ISampleService SampleService { get; }

        ITemplateService TemplateService { get; }

        IVideoService VideoService { get; }

        #endregion Public Properties

        #region Public Methods

        IReadOnlyDictionary<int, string> GetDevices();

        void Initialize();

        void Select(Input input);

        void SelectDevice(string deviceName);

        void SelectFile(string fileName);

        Task StopAsync(Input input = default);

        #endregion Public Methods
    }
}