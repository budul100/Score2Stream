using Core.Events;
using Core.Models;
using Prism.Events;
using Core.Interfaces;
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

        public Clip Clip { get; private set; }

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
            if (Clip != default)
            {
                Clips.Remove(Clip);

                eventAggregator
                    .GetEvent<ClipsChangedEvent>()
                    .Publish();

                SelectClip(default);
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
            Clip = clip;

            eventAggregator
                .GetEvent<ClipSelectedEvent>()
                .Publish(Clip);
        }

        #endregion Private Methods
    }
}