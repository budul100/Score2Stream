using Core.Events.Clip;
using Core.Interfaces;
using Core.Models;
using Prism.Events;
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

        public Service(ITemplateService templateService, IEventAggregator eventAggregator)
        {
            TemplateService = templateService;
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public Clip Clip { get; private set; }

        public List<Clip> Clips { get; } = new List<Clip>();

        public ITemplateService TemplateService { get; }

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

            Select(clip);
        }

        public void Clear()
        {
            if (Clips.Any())
            {
                for (var index = Clips.Count; index > 0; index--)
                {
                    var clip = Clips[index - 1];
                    RemoveClip(clip);
                }

                eventAggregator.GetEvent<ClipsChangedEvent>()
                    .Publish();

                Select(default);
            }
        }

        public void Remove()
        {
            Remove(Clip);
        }

        public void Remove(Clip clip)
        {
            if (clip != default)
            {
                RemoveClip(clip);

                eventAggregator.GetEvent<ClipsChangedEvent>()
                    .Publish();

                Select(default);
            }
        }

        public void Select(Clip clip)
        {
            Clip = clip;

            eventAggregator
                .GetEvent<ClipSelectedEvent>()
                .Publish(Clip);
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
            } while (Clips.Any(c => c.Name == result));

            return result;
        }

        private void RemoveClip(Clip clip)
        {
            if (clip != default)
            {
                TemplateService.Remove(clip.Template);

                Clips.Remove(clip);
            }
        }

        #endregion Private Methods
    }
}