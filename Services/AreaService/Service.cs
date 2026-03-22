using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.AreaService.Extensions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Segment;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;

namespace Score2Stream.AreaService
{
    public class Service
        : IAreaService
    {
        #region Private Fields

        private readonly AreaModifiedEvent areaModifiedEvent;
        private readonly AreasChangedEvent areasChangedEvent;
        private readonly AreaSelectedEvent areaSelectedEvent;
        private readonly AreasOrderedEvent areasOrderedEvent;
        private readonly IDialogService dialogService;
        private readonly IScoreboardService scoreboardService;
        private readonly SegmentSelectedEvent segmentSelectedEvent;
        private readonly ISettingsService<Session> settingsService;

        private ImmutableList<Area> areas = [];
        private int index;
        private Input input;
        private bool orderDescending;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IScoreboardService scoreboardService,
            IDialogService dialogService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.scoreboardService = scoreboardService;
            this.dialogService = dialogService;

            areasChangedEvent = eventAggregator.GetEvent<AreasChangedEvent>();
            areasOrderedEvent = eventAggregator.GetEvent<AreasOrderedEvent>();
            areaSelectedEvent = eventAggregator.GetEvent<AreaSelectedEvent>();
            areaModifiedEvent = eventAggregator.GetEvent<AreaModifiedEvent>();

            segmentSelectedEvent = eventAggregator.GetEvent<SegmentSelectedEvent>();

            eventAggregator.GetEvent<AreaModifiedEvent>().Subscribe(
                action: _ => SaveAreas(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Area Active { get; private set; }

        public Segment ActiveSegment { get; private set; }

        public IReadOnlyList<Area> Areas => areas;

        public bool CanUndo => Active?.X1Last.HasValue == true
            && Active.X2Last.HasValue
            && Active.Y1Last.HasValue
            && Active.Y2Last.HasValue;

        #endregion Public Properties

        #region Public Methods

        public void Add(Area area)
        {
            if (area != default)
            {
                if (Areas.Count >= Constants.MaxCountAreas)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Area),
                        maxCount: Constants.MaxCountAreas);
                }

                area.Segments = area
                    .GetSegments().ToArray();

                area.SetSegments();

                scoreboardService.Bind(
                    area: area,
                    type: area.Type);

                orderDescending = false;

                ImmutableList<Area> add(ImmutableList<Area> c) => !c.Contains(area)
                    ? c.Add(area)
                    : c;

                ImmutableInterlocked.Update(
                    location: ref areas,
                    transformer: add);
            }
        }

        public void Clear()
        {
            if (Areas.Count > 0)
            {
                foreach (var area in Areas)
                {
                    scoreboardService.ReleaseArea(area);
                }

                static ImmutableList<Area> clear(ImmutableList<Area> c) => c.Clear();

                ImmutableInterlocked.Update(
                    location: ref areas,
                    transformer: clear);

                SaveAreas();

                areasChangedEvent.Publish();

                Select();
            }
        }

        public async Task ClearAsync()
        {
            var messageBoxResult = await dialogService.GetMessageBoxResultAsync(
                contentMessage: "Shall all clips be removed?",
                contentTitle: "Remove all clips");

            if (messageBoxResult == ButtonResult.Yes)
            {
                Clear();
            }
        }

        public void Create(int size)
        {
            try
            {
                var area = CreateArea(size);

                Select(area);
            }
            catch (MaxCountExceededException exception)
            {
                dialogService.ShowMessageBoxAsync(
                    contentMessage: exception.Message,
                    contentTitle: "Maximum count exceeded",
                    icon: Icon.Error);
            }
        }

        public void Initialize(Input input)
        {
            this.input = input;
        }

        public void Next(bool backward)
        {
            var next = Areas
                .OrderBy(s => s.Index)
                .GetNext(
                    active: Active,
                    backward: backward);

            if (next != default)
            {
                Select(next);
            }
        }

        public void Order(bool reverseOrder = false)
        {
            var areas = default(IEnumerable<Area>);

            if (orderDescending)
            {
                areas = Areas
                    .OrderByDescending(c => (int)(c.Y1 * Constants.ClipPositionFactor))
                    .ThenByDescending(c => (int)(c.X1 * Constants.ClipPositionFactor)).ToList();
            }
            else
            {
                areas = Areas
                    .OrderBy(c => (int)(c.Y1 * Constants.ClipPositionFactor))
                    .ThenBy(c => (int)(c.X1 * Constants.ClipPositionFactor)).ToList();
            }

            orderDescending = !orderDescending;

            index = 0;

            foreach (var area in areas)
            {
                area.Index = index++;
            }

            areasOrderedEvent.Publish();
        }

        public async Task RemoveAsync()
        {
            if (Active != default)
            {
                var canBeRemoved = !Active.HasDimensions;

                if (!canBeRemoved)
                {
                    var messageBoxResult = await dialogService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the selected clip be removed?",
                        contentTitle: "Remove clip");

                    canBeRemoved = messageBoxResult == ButtonResult.Yes;
                }

                if (canBeRemoved)
                {
                    var next = Areas.Count > 1
                        ? Areas.GetNext(Active)
                        : default;

                    RemoveArea(Active);

                    Select(next);
                }
            }
        }

        public void Resize(double? left, double? widthMin, double? widthFull, double? widthActual,
            double? top, double? heightMin, double? heightFull, double? heightActual)
        {
            var isModified = false;

            Active.HasDimensions = false;

            if ((heightActual ?? 0) > 0
                && (top ?? 0) >= (heightMin ?? 0))
            {
                var y1 = ((top ?? 0) - (heightMin ?? 0)) / heightActual.Value;
                var y2 = ((top ?? 0) - (heightMin ?? 0) + (heightFull ?? 0)) / heightActual.Value;

                isModified |= ResizeVertical(
                    y1: y1,
                    y2: y2);
            }

            if ((widthActual ?? 0) > 0
                && (left ?? 0) >= (widthMin ?? 0))
            {
                var x1 = ((left ?? 0) - (widthMin ?? 0)) / widthActual.Value;
                var x2 = ((left ?? 0) - (widthMin ?? 0) + (widthFull ?? 0)) / widthActual.Value;

                isModified |= ResizeHorizontal(
                    x1: x1,
                    x2: x2);
            }

            if (isModified)
            {
                Active.SetSegments();

                areaModifiedEvent.Publish(Active);
            }
        }

        public void Select(Area area)
        {
            if (Active != area)
            {
                Active = area;

                areaSelectedEvent.Publish(Active);

                if (ActiveSegment != default
                    && ActiveSegment.Area != Active)
                {
                    Select();
                }
            }
        }

        public void Select(Segment segment = default)
        {
            if (ActiveSegment != segment)
            {
                ActiveSegment = segment;

                segmentSelectedEvent.Publish(ActiveSegment);

                if (Active != ActiveSegment?.Area)
                {
                    Active = ActiveSegment?.Area;

                    areaSelectedEvent.Publish(Active);
                }
            }
        }

        public void Undo()
        {
            if (CanUndo)
            {
                var isModified = false;

                isModified |= ResizeVertical(
                    y1: Active.Y1Last,
                    y2: Active.Y2Last);

                isModified |= ResizeHorizontal(
                    x1: Active.X1Last,
                    x2: Active.X2Last);

                if (isModified)
                {
                    Active.SetSegments();

                    areaModifiedEvent.Publish(Active);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Area CreateArea(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentException(
                    message: "The size of an area must be greater than zero.",
                    paramName: nameof(size));
            }

            var name = Areas.GetNextName();

            var result = new Area()
            {
                Name = name,
                Size = size,
            };

            if (Active != default)
            {
                result.NoiseRemoval = Active.NoiseRemoval;
                result.ThresholdMonochrome = Active.ThresholdMonochrome;
            }

            Add(result);

            SaveAreas();

            areasChangedEvent.Publish();

            return result;
        }

        private void RemoveArea(Area area)
        {
            if (area != default)
            {
                scoreboardService.ReleaseArea(area);

                ImmutableList<Area> remove(ImmutableList<Area> c) => c.Contains(area)
                    ? c.Remove(area)
                    : c;

                ImmutableInterlocked.Update(
                    location: ref areas,
                    transformer: remove);

                SaveAreas();

                areasChangedEvent.Publish();
            }
        }

        private bool ResizeHorizontal(double? x1, double? x2)
        {
            var result = false;

            if (x1.HasValue
                && x2.HasValue)
            {
                if (Active.X1 != x1 || Active.X2 != x2)
                {
                    Active.X1Last = Active.X1;
                    Active.X2Last = Active.X2;

                    Active.X1 = x1.Value;
                    Active.X2 = x2.Value;

                    result = true;
                }

                Active.HasDimensions = true;
            }

            return result;
        }

        private bool ResizeVertical(double? y1, double? y2)
        {
            var result = false;

            if (y1.HasValue
                && y2.HasValue)
            {
                if (Active.Y1 != y1 || Active.Y2 != y2)
                {
                    Active.Y1Last = Active.Y1;
                    Active.Y2Last = Active.Y2;

                    Active.Y1 = y1.Value;
                    Active.Y2 = y2.Value;

                    result = true;
                }

                Active.HasDimensions = true;
            }

            return result;
        }

        private void SaveAreas()
        {
            input.Areas = Areas?.ToList();

            settingsService.Save();
        }

        #endregion Private Methods
    }
}