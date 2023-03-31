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

        public event EventHandler OnClipDefinedEvent;

        public event EventHandler OnClipsChangedEvent;

        public event EventHandler OnClipsUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public List<Clip> Clips { get; } = new List<Clip>();

        public Clip Selection { get; private set; }

        #endregion Public Properties

        #region Public Methods

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

            Select(clip);
        }

        public void Define()
        {
            if (Selection != default)
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
            if (Selection != default)
            {
                Unselect();

                Clips.Remove(Selection);

                OnClipsChangedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        public void Select(Clip clip)
        {
            Selection = clip;

            Update();
        }

        public void Update()
        {
            OnClipsUpdatedEvent?.Invoke(
                sender: this,
                e: default);
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

        private void Unselect()
        {
            Selection = default;

            Update();
        }

        #endregion Private Methods
    }
}