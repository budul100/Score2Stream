using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface IClipService
    {
        #region Public Events

        event EventHandler OnClipActivatedEvent;

        event EventHandler OnClipDimensionedEvent;

        event EventHandler OnClipsChangedEvent;

        #endregion Public Events

        #region Public Properties

        Clip Active { get; }

        List<Clip> Clips { get; }

        #endregion Public Properties

        #region Public Methods

        void Activate(Clip clip);

        void Add();

        void Save();

        #endregion Public Methods
    }
}