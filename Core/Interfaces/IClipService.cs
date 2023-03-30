using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface IClipService
    {
        #region Public Events

        event EventHandler OnClipActivatedEvent;

        event EventHandler OnClipDefinedEvent;

        event EventHandler OnClipsChangedEvent;

        #endregion Public Events

        #region Public Properties

        Clip Active { get; }

        List<Clip> Clips { get; }

        #endregion Public Properties

        #region Public Methods

        void Activate(Clip clip);

        void Add();

        void Define();

        bool IsUniqueName(string name);

        void Remove();

        #endregion Public Methods
    }
}