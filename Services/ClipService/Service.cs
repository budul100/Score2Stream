using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClipService
{
    public class Service
        : IClipService
    {
        #region Public Events

        public event EventHandler OnClipActivatedEvent;

        public event EventHandler OnClipDefinedEvent;

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
            var name = GetName();

            var clip = new Clip()
            {
                Name = name
            };

            Clips.Add(clip);

            OnClipsChangedEvent?.Invoke(
                sender: this,
                e: default);

            Activate(clip);
        }

        public void Define()
        {
            if (Active != default)
            {
                OnClipDefinedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        public bool IsUniqueName(string name)
        {
            var result = !Clips.Any(c => c.Name == name);

            return result;
        }

        public void Remove()
        {
            if (Active != default)
            {
                Clips.Remove(Active);

                OnClipsChangedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private string GetName()
        {
            var index = 0;

            string result;

            do
            {
                result = $"Clip{++index}";
            } while (!IsUniqueName(result));

            return result;
        }

        #endregion Private Methods
    }
}