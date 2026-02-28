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

        void Initialize();

        Task SelectAsync(Input input);

        Task StopAsync();

        void Update();

        #endregion Public Methods
    }
}