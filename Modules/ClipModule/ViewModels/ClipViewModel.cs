using Core.Enums;
using Core.Events.Clip;
using Core.Events.Template;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ClipModule.ViewModels
{
    public class ClipViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IScoreboardService scoreboardService;

        private IClipService clipService;
        private bool isActive;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(IScoreboardService scoreboardService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.eventAggregator = eventAggregator;

            OnClickCommand = new DelegateCommand(
                executeMethod: () => clipService?.Select(Clip));

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => IsActive = c == Clip,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => UpdateTemplates(),
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

            InitializeTypes();
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Bitmap => Clip?.Bitmap;

        public Clip Clip { get; private set; }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public DelegateCommand OnClickCommand { get; }

        public Template Template
        {
            get { return Clip?.Template; }
            set
            {
                Clip.Template = value;

                RaisePropertyChanged(nameof(Template));
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
                    Clip.ThresholdMonochrome = value;

                    eventAggregator
                        .GetEvent<ClipUpdatedEvent>()
                        .Publish(Clip);
                }

                RaisePropertyChanged(nameof(ThresholdMonochrome));
            }
        }

        public ClipType Type
        {
            get { return Clip?.Type ?? ClipType.None; }
            set
            {
                scoreboardService.SetClip(
                    contentType: value,
                    clip: Clip);

                RaisePropertyChanged(nameof(Type));
            }
        }

        public ObservableCollection<ClipType> Types { get; } = new ObservableCollection<ClipType>();

        public string Value => !string.IsNullOrWhiteSpace(Clip?.Value)
            ? $"=> {Clip.Value}"
            : default;

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Clip clip, IClipService clipService)
        {
            this.Clip = clip;
            this.clipService = clipService;

            UpdateTemplates();
        }

        #endregion Public Methods

        #region Private Methods

        private void InitializeTypes()
        {
            foreach (ClipType clipType in Enum.GetValues(typeof(ClipType)))
            {
                Types.Add(clipType);
            }

            RaisePropertyChanged(nameof(Types));
        }

        private void SelectTemplate()
        {
            Template = Clip.Template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        private void UpdateImage()
        {
            RaisePropertyChanged(nameof(Bitmap));
            RaisePropertyChanged(nameof(Value));
        }

        private void UpdateTemplates()
        {
            var template = Clip?.Template;
            var templates = clipService?.TemplateService?.Templates;

            Template = default;

            var toBeRemoveds = Templates
                .Where(t => templates?.Contains(t) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Templates.Remove(toBeRemoved);
            }

            var toBeAddeds = templates
                .Where(t => !Templates.Contains(t)).ToArray();

            Templates.AddRange(toBeAddeds);

            Template = template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        #endregion Private Methods
    }
}