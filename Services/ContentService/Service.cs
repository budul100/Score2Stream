using Core.Enums;
using Core.Events.Contents;
using Core.Interfaces;
using Core.Models;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ContentService
{
    public class Service
        : IContentService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            InitializeContents();
        }

        #endregion Public Constructors

        #region Public Properties

        public IDictionary<ClipType, Clip> Contents { get; } = new Dictionary<ClipType, Clip>();

        #endregion Public Properties

        #region Public Methods

        public void Set(ClipType clipType, Clip clip)
        {
            if (clipType == ClipType.None)
            {
                clip.Type = ClipType.None;
            }
            else if (Contents[clipType] != clip)
            {
                if (Contents[clipType] != default)
                {
                    Contents[clipType].Type = ClipType.None;
                }

                Contents[clipType] = clip;
                Contents[clipType].Type = clipType;

                eventAggregator
                    .GetEvent<ContentUpdatedEvent>()
                    .Publish(clipType);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void InitializeContents()
        {
            var relevants = Enum.GetValues(typeof(ClipType))
                .OfType<ClipType>()
                .Where(t => t != ClipType.None).ToArray();

            foreach (var relevant in relevants)
            {
                Contents.Add(
                    key: relevant,
                    value: default);
            }
        }

        #endregion Private Methods
    }
}