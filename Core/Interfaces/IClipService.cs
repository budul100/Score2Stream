using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;

namespace ScoreboardOCR.Core.Interfaces
{
    public interface IClipService
    {
        #region Public Events

        event EventHandler OnClipDefinedEvent;

        event EventHandler OnClipsChangedEvent;

        event EventHandler OnClipSelectedEvent;

        event EventHandler OnServiceDeactivatedEvent;

        #endregion Public Events

        #region Public Properties

        List<Clip> Clips { get; }

        Clip Selection { get; }

        #endregion Public Properties

        #region Public Methods

        void Add();

        void Deactivate();

        void Define();

        bool IsUniqueName(string name);

        void Remove();

        void Select(Clip clip);

        #endregion Public Methods
    }
}