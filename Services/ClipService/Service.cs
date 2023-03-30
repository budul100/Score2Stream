using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;

namespace ClipService
{
    public class Service
        : IClipService
    {
        #region Public Events

        public event EventHandler OnClipActivatedEvent;

        public event EventHandler OnClipDimensionedEvent;

        public event EventHandler OnClipsChangedEvent;

        #endregion Public Events

        #region Public Properties

        public Clip Active { get; private set; }

        public List<Clip> Clips { get; } = new List<Clip>();

        #endregion Public Properties

        #region Public Methods

        public void Activate(Clip clip)
        {
            Active = clip;

            OnClipActivatedEvent?.Invoke(
                sender: this,
                e: default);
        }

        public void Add()
        {
            var clip = new Clip();

            Clips.Add(clip);

            OnClipsChangedEvent?.Invoke(
                sender: this,
                e: default);

            Activate(clip);
        }

        public void Save()
        {
            if (Active != default)
            {
                OnClipDimensionedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        #endregion Public Methods
    }
}