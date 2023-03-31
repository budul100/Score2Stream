using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface ITemplateService
    {
        #region Public Events

        event EventHandler OnSamplesChangedEvent;

        #endregion Public Events

        #region Public Properties

        Template Selection { get; }

        List<Template> Templates { get; }

        #endregion Public Properties

        #region Public Methods

        void AddSample();

        void RemoveSample();

        void RemoveTemplate();

        void SelectSample(Sample sample);

        #endregion Public Methods
    }
}