using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Score2Stream.AreaModule.Extensions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.AreaModule.ViewModels
{
    public class AreaViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly AreaModifiedEvent areaModifiedEvent;
        private readonly IContainerProvider containerProvider;
        private readonly IScoreboardService scoreboardService;

        private Area area;
        private IAreaService areaService;
        private bool isActive;
        private bool isInitializing;
        private bool isUpdatingType;

        #endregion Private Fields

        #region Public Constructors

        public AreaViewModel(IScoreboardService scoreboardService, IContainerProvider containerProvider,
            IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.containerProvider = containerProvider;

            OnSelectionCommand = new DelegateCommand(
                executeMethod: () => ActivateArea());

            areaModifiedEvent = eventAggregator.GetEvent<AreaModifiedEvent>();

            eventAggregator.GetEvent<AreaSelectedEvent>().Subscribe(
                action: a => IsActive = a == area,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => UpdateType(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => SelectTemplate(),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: () => UpdateTemplates(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<ClipViewModel> Clips { get; } = new ObservableCollection<ClipViewModel>();

        public int Index => area.Index;

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public int NoiseRemoval
        {
            get { return area?.NoiseRemoval ?? 0; }
            set
            {
                if (value >= 0
                    && value <= 10
                    && area?.NoiseRemoval != value)
                {
                    ActivateArea();

                    area.NoiseRemoval = value;

                    RaisePropertyChanged(nameof(NoiseRemoval));

                    areaModifiedEvent.Publish(area);
                }
            }
        }

        public DelegateCommand OnSelectionCommand { get; }

        public Template Template
        {
            get { return area?.Template; }
            set
            {
                if (value != default
                    && area.Template != value)
                {
                    ActivateArea();

                    area.Template = value;
                    area.TemplateName = value.Name;

                    RaisePropertyChanged(nameof(Template));
                }
            }
        }

        public ObservableCollection<Template> Templates { get; } = new ObservableCollection<Template>();

        public int ThresholdMonochrome
        {
            get { return area?.ThresholdMonochrome ?? 0; }
            set
            {
                if (value >= 0
                    && value <= Constants.ThresholdMax
                    && area?.ThresholdMonochrome != value)
                {
                    ActivateArea();

                    area.ThresholdMonochrome = value;

                    RaisePropertyChanged(nameof(ThresholdMonochrome));

                    areaModifiedEvent.Publish(area);
                }
            }
        }

        public AreaType Type
        {
            get { return area?.Type ?? AreaType.None; }
            set
            {
                if (!isUpdatingType)
                {
                    ActivateArea();

                    isUpdatingType = true;

                    scoreboardService.BindArea(
                        area: area,
                        type: value);

                    isUpdatingType = false;
                }
            }
        }

        public IEnumerable<AreaType> Types { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Area area, IAreaService areaService)
        {
            isInitializing = true;

            this.areaService = areaService;
            this.area = area;

            IsActive = areaService.Area == area;

            Type = area.Type;
            Types = area.Size
                .GetAreaTypes().ToArray();

            RaisePropertyChanged(nameof(Types));

            UpdateType();

            UpdateClips(area);

            isInitializing = false;

            if (areaService.Area == area)
            {
                ActivateArea();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void ActivateArea()
        {
            if (!isInitializing
                && areaService?.Area != area)
            {
                areaService?.Select(area);
            }
        }

        private void SelectTemplate()
        {
            Template = area.Template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        private void UpdateClips(Area area)
        {
            foreach (var clip in area.Clips)
            {
                var current = containerProvider.Resolve<ClipViewModel>();

                current.Initialize(clip);

                Clips.Add(current);
            }
        }

        private void UpdateTemplates()
        {
            var template = area.Template;

            Template = default;

            var toBeRemoveds = Templates
                .Where(t => areaService?.TemplateService?.Templates?.Contains(t) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Templates.Remove(toBeRemoved);
            }

            if (areaService.TemplateService?.Templates != default)
            {
                var toBeAddeds = areaService?.TemplateService?.Templates
                    .Where(t => !Templates.Contains(t)).ToArray();

                Templates.AddRange(toBeAddeds);
            }

            Template = template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        private void UpdateType()
        {
            UpdateTemplates();

            RaisePropertyChanged(nameof(Type));
        }

        #endregion Private Methods
    }
}