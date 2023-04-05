using Core.Models;
using System.Collections.Generic;

namespace Core.Interfaces
{
    public interface ITemplateService
    {
        #region Public Properties

        ISampleService SampleService { get; }

        Template Template { get; }

        List<Template> Templates { get; }

        #endregion Public Properties

        #region Public Methods

        void Add(Clip clip);

        void Remove();

        void Remove(Template template);

        void Select(Template template);

        #endregion Public Methods
    }
}