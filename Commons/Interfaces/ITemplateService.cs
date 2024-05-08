using Score2Stream.Commons.Models.Contents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Score2Stream.Commons.Interfaces
{
    public interface ITemplateService
    {
        #region Public Properties

        ISampleService SampleService { get; }

        Template Template { get; }

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