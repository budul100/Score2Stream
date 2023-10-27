using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Extensions;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Score2Stream.ClipService
{
    public class Service
        : IClipService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IMessageBoxService messageBoxService;
        private readonly IScoreboardService scoreboardService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IScoreboardService scoreboardService, ITemplateService templateService,
            IMessageBoxService messageBoxService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;

            TemplateService = templateService;
        }

        #endregion Public Constructors

        #region Public Properties

        public Clip Active { get; private set; }

        public List<Clip> Clips { get; } = new List<Clip>();

        public ITemplateService TemplateService { get; }

        #endregion Public Properties

        #region Public Methods

        public void Add(Clip clip)
        {
            if (clip != default)
            {
                Clips.Add(clip);

                scoreboardService.SetClip(
                    clip: clip,
                    clipType: clip.Type);
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

        public async Task ClearAsync()
        {
            var result = await messageBoxService.GetMessageBoxResultAsync(
                contentMessage: "Shall all clips be removed?",
                contentTitle: "Remove all clips");

            if (result == ButtonResult.Yes)
            {
                Clear();
            }
        }

        public void Create()
        {
            var clip = GetClip();

            Add(clip);

            eventAggregator
                .GetEvent<ClipsChangedEvent>()
                .Publish();

            Select(clip);
        }

        public void Next(bool backward)
        {
            var next = Clips.GetNext(
                active: Active,
                backward: backward);

            if (next != default)
            {
                Select(next);
            }
        }

        public async Task RemoveAsync()
        {
            if (Active != default)
            {
                var result = ButtonResult.Yes;

                if (Active.HasDimensions)
                {
                    result = await messageBoxService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the selected clip be removed?",
                        contentTitle: "Remove clip");
                }

                if (result == ButtonResult.Yes)
                {
                    var next = Clips.GetNext(Active);

                    RemoveClip(Active);

                    eventAggregator.GetEvent<ClipsChangedEvent>()
                        .Publish();

                    Select(next);
                }
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

        private Clip GetClip()
        {
            var name = Clips.GetNextName();

            var result = new Clip()
            {
                Name = name,
            };

            if (Active != default)
            {
                result.Template = Active.Template;
                result.NoiseRemoval = Active.NoiseRemoval;
                result.ThresholdMonochrome = Active.ThresholdMonochrome;
            }

            return result;
        }

        private void RemoveClip(Clip clip)
        {
            if (clip != default)
            {
                scoreboardService.RemoveClip(clip);

                Clips.Remove(clip);
            }
        }

        #endregion Private Methods
    }
}