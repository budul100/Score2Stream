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
                action: _ => UpdateStatus(),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => UpdateValues(),
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

        public Area Area { get; private set; }

        public ObservableCollection<SegmentViewModel> Clips { get; } = new ObservableCollection<SegmentViewModel>();

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public int NoiseRemoval
        {
            get { return Area?.NoiseRemoval ?? 0; }
            set
            {
                if (value >= 0
                    && value <= 10
                    && Area?.NoiseRemoval != value)
                {
                    ActivateArea();

                    Area.NoiseRemoval = value;

                    RaisePropertyChanged(nameof(NoiseRemoval));

                    areaModifiedEvent.Publish(Area);
                }
            }
        }

        public DelegateCommand OnSelectionCommand { get; }

        public Template Template
        {
            get { return Area?.Template; }
            set
            {
                if (value != default
                    && Area.Template != value)
                {
                    ActivateArea();

                    Area.Template = value;
                    Area.TemplateName = value.Name;

                    RaisePropertyChanged(nameof(Template));
                }
            }
        }

        public ObservableCollection<Template> Templates { get; } = new ObservableCollection<Template>();

        public int ThresholdMonochrome
        {
            get { return Area?.ThresholdMonochrome ?? 0; }
            set
            {
                if (value >= 0
                    && value <= Constants.ThresholdMax
                    && Area?.ThresholdMonochrome != value)
                {
                    ActivateArea();

                    Area.ThresholdMonochrome = value;

                    RaisePropertyChanged(nameof(ThresholdMonochrome));

                    areaModifiedEvent.Publish(Area);
                }
            }
        }

        public AreaType Type
        {
            get { return Area?.Type ?? AreaType.None; }
            set
            {
                if (!isUpdatingType)
                {
                    ActivateArea();

                    isUpdatingType = true;

                    scoreboardService.BindArea(
                        area: Area,
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

            Area = area;
            this.areaService = areaService;

            Type = area.Type;
            Types = area.Size
                .GetAreaTypes().ToArray();

            RaisePropertyChanged(nameof(Types));

            UpdateValues();
            UpdateClips();

            isInitializing = false;

            UpdateStatus();
        }

        #endregion Public Methods

        #region Private Methods

        private void ActivateArea()
        {
            if (!isInitializing)
            {
                areaService?.Select(Area);
            }
        }

        private void SelectTemplate()
        {
            Template = Area.Template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        private void UpdateClips()
        {
            foreach (var clip in Area.Segments)
            {
                var current = containerProvider.Resolve<SegmentViewModel>();

                current.Initialize(clip);

                Clips.Add(current);
            }
        }

        private void UpdateStatus()
        {
            IsActive = areaService.Area == Area;
        }

        private void UpdateTemplates()
        {
            var template = Area.Template;

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

        private void UpdateValues()
        {
            UpdateTemplates();

            RaisePropertyChanged(nameof(Type));
        }

        #endregion Private Methods
    }
}