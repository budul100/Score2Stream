using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.Core;
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

        private int index;

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

        public List<Clip> Clips { get; private set; } = new List<Clip>();

        public ITemplateService TemplateService { get; }

        public bool UndoSizePossible => Active?.X1Last.HasValue == true
            && Active.X2Last.HasValue
            && Active.Y1Last.HasValue
            && Active.Y2Last.HasValue;

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

        public void Order()
        {
            Clips = Clips
                .OrderBy(c => (int)(c.Y1 * Constants.ClipPositionFactor))
                .ThenBy(c => (int)(c.X1 * Constants.ClipPositionFactor)).ToList();

            index = 0;

            foreach (var clips in Clips)
            {
                clips.Index = index++;
            }

            eventAggregator.GetEvent<ClipsOrderedEvent>().Publish();
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

        public void UndoSize()
        {
            if (UndoSizePossible)
            {
                var lastX1 = Active.X1;
                var lastX2 = Active.X2;
                var lastY1 = Active.Y1;
                var lastY2 = Active.Y2;

                Active.X1 = Active.X1Last.Value;
                Active.X2 = Active.X2Last.Value;
                Active.Y1 = Active.Y1Last.Value;
                Active.Y2 = Active.Y2Last.Value;

                Active.X1Last = lastX1;
                Active.X2Last = lastX2;
                Active.Y1Last = lastY1;
                Active.Y2Last = lastY2;

                Active.HasDimensions = true;

                eventAggregator.GetEvent<ClipUpdatedEvent>().Publish(Active);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Clip GetClip()
        {
            var name = Clips.GetNextName();

            var result = new Clip()
            {
                Name = name,
                Index = index++,
            };

            if (Active != default)
            {
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