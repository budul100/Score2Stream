using System.Collections.Generic;
using System.Threading.Tasks;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface ITemplateService
    {
        #region Public Properties

        Template Active { get; }

        ISampleService SampleService { get; }

        List<Template> Templates { get; }

        #endregion Public Properties

        #region Public Methods

        void Add(Template template);

        void Create();

        void Initialize();

        Task RemoveAsync();

        void Select(Template template);

        #endregion Public Methods
    }
}