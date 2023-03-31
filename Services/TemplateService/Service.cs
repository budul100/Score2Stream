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

        public event EventHandler OnTemplateActivatedEvent;

        public event EventHandler OnTemplatesChangedEvent;

        #endregion Public Events

        #region Public Properties

        public Template Active { get; set; }

        public List<Template> Templates { get; } = new List<Template>();

        #endregion Public Properties

        #region Public Methods

        public void Activate(Clip clip)
        {
            Active = Templates
                .SingleOrDefault(t => t.Clip == clip);

            if (Active == default)
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

                Active = current;
            }

            OnTemplateActivatedEvent?.Invoke(
                sender: this,
                e: default);
        }

        public void Remove()
        {
            if (Active != default)
            {
                Templates.Remove(Active);
                Active = default;

                OnTemplatesChangedEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        #endregion Public Methods
    }
}