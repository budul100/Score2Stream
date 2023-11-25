using Score2Stream.Commons.Models.Contents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
{
    public interface IInputService
    {
        #region Public Properties

        Input Active { get; }

        IClipService ClipService { get; }

        HashSet<Input> Inputs { get; }

        bool IsActive { get; }

        ISampleService SampleService { get; }

        ITemplateService TemplateService { get; }

        IVideoService VideoService { get; }

        #endregion Public Properties

        #region Public Methods

        void Initialize();

        void Select(Input input);

        void Select(string fileName);

        Task StopAsync();

        void Update();

        #endregion Public Methods
    }
}