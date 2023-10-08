using Prism.Events;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.ClipService
{
    public class Service
        : IClipService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IScoreboardService scoreboardService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IScoreboardService scoreboardService, ITemplateService templateService,
            IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.eventAggregator = eventAggregator;

            TemplateService = templateService;
        }

        #endregion Public Constructors

        #region Public Properties

        public Clip Active { get; private set; }

        public List<Clip> Clips { get; } = new List<Clip>();

        public ITemplateService TemplateService { get; }

        #endregion Public Properties

        #region Public Methods

        public void Add()
        {
            var name = GetName();

            var clip = new Clip()
            {
                Name = name,
                Template = Active?.Template
            };

            if (Active != default)
            {
                clip.NoiseRemoval = Active.NoiseRemoval;
                clip.ThresholdMonochrome = Active.ThresholdMonochrome;
            }

            Add(clip);

            Select(clip);
        }

        public void Add(Clip clip)
        {
            if (clip != default)
            {
                Clips.Add(clip);

                scoreboardService.SetClip(
                    clip: clip,
                    clipType: clip.Type);

                eventAggregator.GetEvent<ClipsChangedEvent>().Publish();
            }
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
            Remove(Active);
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
            Active = Active != clip
                ? clip
                : default;

            eventAggregator
                .GetEvent<ClipSelectedEvent>()
                .Publish(Active);
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
                if (clip.Template?.Clip == clip)
                {
                    TemplateService.Remove(clip.Template);
                }

                Clips.Remove(clip);

                scoreboardService.RemoveClip(clip);
            }
        }

        #endregion Private Methods
    }
}