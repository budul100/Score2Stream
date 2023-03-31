using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateService
{
    public class Service
        : ITemplateService
    {
        #region Public Events

        public event EventHandler OnServiceDeactivatedEvent;

        public event EventHandler OnTemplatesChangedEvent;

        public event EventHandler OnTemplateSelectedEvent;

        #endregion Public Events

        #region Public Properties

        public bool IsActive { get; set; }

        public Template Selection { get; set; }

        public List<Template> Templates { get; } = new List<Template>();

        #endregion Public Properties

        #region Public Methods

        public void Deactivate()
        {
            Unselect();

            OnServiceDeactivatedEvent?.Invoke(
                sender: this,
                e: default);
        }

        public void Remove()
        {
            if (Selection != default)
            {
                Templates.Remove(Selection);
                Selection = default;

                OnTemplatesChangedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        public void Select(Clip clip)
        {
            Selection = Templates
                .SingleOrDefault(t => t.Clip == clip);

            if (Selection == default)
            {
                var current = new Template
                {
                    Clip = clip,
                };

                Templates.Add(
                    item: current);

                OnTemplatesChangedEvent?.Invoke(
                    sender: this,
                    e: default);

                Selection = current;
            }

            OnTemplateSelectedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Public Methods

        #region Private Methods

        private void Unselect()
        {
            Selection = default;

            OnTemplateSelectedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Private Methods
    }
}