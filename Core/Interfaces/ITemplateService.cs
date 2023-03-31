using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface ITemplateService
    {
        #region Public Events

        event EventHandler OnTemplatesChangedEvent;

        event EventHandler OnTemplateSelectedEvent;

        #endregion Public Events

        #region Public Properties

        Template Selection { get; set; }

        List<Template> Templates { get; }

        #endregion Public Properties

        #region Public Methods

        void Remove();

        void Select(Clip clip);

        #endregion Public Methods
    }
}