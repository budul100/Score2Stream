using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface ITemplateService
    {
        #region Public Events

        event EventHandler OnTemplateActivatedEvent;

        event EventHandler OnTemplatesChangedEvent;

        #endregion Public Events

        #region Public Properties

        Template Active { get; set; }

        List<Template> Templates { get; }

        #endregion Public Properties

        #region Public Methods

        void Activate(Clip clip);

        void Remove();

        #endregion Public Methods
    }
}