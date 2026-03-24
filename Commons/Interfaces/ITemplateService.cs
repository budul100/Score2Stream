using System.Collections.Generic;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface ITemplateService
    {
        #region Public Properties

        Template Active { get; }

        ISampleService SampleService { get; }

        IReadOnlyList<Template> Templates { get; }

        #endregion Public Properties

        #region Public Methods

        void Create();

        void Initialize();

        void Remove(Template template);

        void Select(Template template);

        #endregion Public Methods
    }
}