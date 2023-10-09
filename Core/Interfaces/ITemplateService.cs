using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Score2Stream.Core.Interfaces
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

        Task RemoveAsync();

        void Select(Template template);

        #endregion Public Methods
    }
}