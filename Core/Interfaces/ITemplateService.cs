using ScoreboardOCR.Core.Models;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface ITemplateService
    {
        #region Public Properties

        Sample Sample { get; }

        Template Template { get; }

        List<Template> Templates { get; }

        #endregion Public Properties

        #region Public Methods

        void AddSample();

        void RemoveSample();

        void RemoveTemplate();

        #endregion Public Methods
    }
}