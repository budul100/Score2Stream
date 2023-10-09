using Avalonia.Media.Imaging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Events.Clip;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Score2Stream.ClipModule.ViewModels
{
    public class ClipViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IScoreboardService scoreboardService;
        private readonly IEnumerable<ClipType> types;

        private IClipService clipService;
        private bool isActive;
        private bool isInitializing;
        private bool isUpdatingType;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.eventAggregator = eventAggregator;

            OnSelectionCommand = new DelegateCommand(
                executeMethod: () => clipService?.Select(Clip));

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => IsActive = c == Clip,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => UpdateClip(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => SelectTemplate(),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: () => UpdateTemplates(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => UpdateImage(),
                keepSubscriberReferenceAlive: true);

            types = Core.Extensions.EnumExtensions
                .GetValues<ClipType>().ToArray();
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => Clip?.Bitmap;

        public Clip Clip { get; private set; }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public int NoiseRemoval
        {
            get { return Clip?.NoiseRemoval ?? 0; }
            set
            {
                if (value >= 0
                    && value <= 10
                    && Clip?.NoiseRemoval != value)
                {
                    if (!isInitializing
                        && clipService?.Active != Clip)
                    {
                        clipService?.Select(Clip);
                    }

                    Clip.NoiseRemoval = value;

                    eventAggregator.GetEvent<ClipUpdatedEvent>().Publish(
                        payload: Clip);
                }

                RaisePropertyChanged(nameof(NoiseRemoval));
            }
        }

        public DelegateCommand OnSelectionCommand { get; }

        public Template Template
        {
            get { return Clip?.Template; }
            set
            {
                if (value != default
                    && Clip.Template != value)
                {
                    if (!isInitializing
                        && clipService?.Active != Clip)
                    {
                        clipService?.Select(Clip);
                    }

                    Clip.Template = value;
                    Clip.TemplateName = value.Name;

                    RaisePropertyChanged(nameof(Template));
                }
            }
        }

        public ObservableCollection<Template> Templates { get; } = new ObservableCollection<Template>();

        public int ThresholdMonochrome
        {
            get { return Clip?.ThresholdMonochrome ?? 0; }
            set
            {
                if (value >= 0
                    && value <= 100
                    && Clip?.ThresholdMonochrome != value)
                {
                    if (!isInitializing
                        && clipService?.Active != Clip)
                    {
                        clipService?.Select(Clip);
                    }

                    Clip.ThresholdMonochrome = value;

                    eventAggregator.GetEvent<ClipUpdatedEvent>().Publish(
                        payload: Clip);
                }

                RaisePropertyChanged(nameof(ThresholdMonochrome));
            }
        }

        public ClipType Type
        {
            get { return Clip?.Type ?? ClipType.None; }
            set
            {
                if (!isUpdatingType)
                {
                    if (!isInitializing
                        && clipService?.Active != Clip)
                    {
                        clipService?.Select(Clip);
                    }

                    isUpdatingType = true;

                    scoreboardService.SetClip(
                        clip: Clip,
                        clipType: value);

                    isUpdatingType = false;
                }
            }
        }

        public ObservableCollection<ClipType> Types { get; } = new ObservableCollection<ClipType>();

        public string Value => !string.IsNullOrWhiteSpace(Clip?.Value)
            ? $"=> {Clip.Value} (Similarity: {Clip.Similarity}%)"
            : $"=> -/- (Similarity: {Clip.Similarity}%)";

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Clip clip, IClipService clipService)
        {
            isInitializing = true;

            this.clipService = clipService;
            this.IsActive = clipService.Active == clip;

            this.Clip = clip;
            this.Type = clip.Type;

            UpdateClip();

            isInitializing = false;
        }

        #endregion Public Methods

        #region Private Methods

        private void SelectTemplate()
        {
            Template = Clip.Template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        private void UpdateClip()
        {
            UpdateTypes();
            UpdateTemplates();
        }

        private void UpdateImage()
        {
            RaisePropertyChanged(nameof(Bitmap));
            RaisePropertyChanged(nameof(Value));
        }

        private void UpdateTemplates()
        {
            var template = Clip.Template;

            Template = default;

            var toBeRemoveds = Templates
                .Where(t => clipService?.TemplateService?.Templates?.Contains(t) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Templates.Remove(toBeRemoved);
            }

            if (clipService.TemplateService?.Templates != default)
            {
                var toBeAddeds = clipService?.TemplateService?.Templates
                    .Where(t => !Templates.Contains(t)).ToArray();

                Templates.AddRange(toBeAddeds);
            }

            Template = template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        private void UpdateTypes()
        {
            var index = 0;

            foreach (var type in types)
            {
                if (type == Clip.Type || scoreboardService.ClipTypes.Contains(type))
                {
                    if (!Types.Contains(type))
                    {
                        Types.Insert(
                            index: index,
                            item: type);
                    }

                    index++;
                }
                else if (Types.Contains(type))
                {
                    Types.Remove(type);
                }
            }

            RaisePropertyChanged(nameof(Types));
            RaisePropertyChanged(nameof(Type));
        }

        #endregion Private Methods
    }
}