using Core.Events;
using Prism.Events;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ClipService
{
    public class Service
        : IClipService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<SelectClipEvent>().Subscribe(
                action: c => SelectClip(c),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

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

            eventAggregator
                .GetEvent<ClipsChangedEvent>()
                .Publish();

            SelectClip(clip);
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
                var current = Selection;

                SelectClip(default);

                Clips.Remove(current);

                eventAggregator
                    .GetEvent<ClipsChangedEvent>()
                    .Publish();
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

        private void SelectClip(Clip clip)
        {
            Selection = clip;

            eventAggregator
                .GetEvent<ClipSelectedEvent>()
                .Publish(Selection);
        }

        #endregion Private Methods
    }
}