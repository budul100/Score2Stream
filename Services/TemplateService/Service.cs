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
        #region Private Fields

        private readonly IClipService clipService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IClipService clipService)
        {
            this.clipService = clipService;
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler OnTemplatesChangedEvent;

        public event EventHandler OnTemplatesUpdatedEvent;

        #endregion Public Events

        #region Public Properties

        public bool IsActive { get; set; }

        public Template Selection { get; set; }

        public List<Template> Templates { get; } = new List<Template>();

        #endregion Public Properties

        #region Public Methods

        public void Remove()
        {
            if (Selection != default)
            {
                var current = Selection;

                Unselect();

                Templates.Remove(current);

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

            clipService.Select(Selection.Clip);

            OnTemplatesUpdatedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Public Methods

        #region Private Methods

        private void Unselect()
        {
            Selection = default;

            OnTemplatesUpdatedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Private Methods
    }
}