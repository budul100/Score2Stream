using MsBox.Avalonia.Enums;
using Prism.Events;
using Score2Stream.AreaService.Extensions;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Area;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

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
        private readonly SegmentSelectedEvent clipSelectedEvent;
        private readonly IDialogService dialogService;
        private readonly IScoreboardService scoreboardService;

        private int index;
        private bool orderDescending;

        #endregion Private Fields

        #region Public Constructors

        public Service(IScoreboardService scoreboardService, ITemplateService templateService,
            IDialogService dialogService, IEventAggregator eventAggregator)
        {
            this.scoreboardService = scoreboardService;
            this.dialogService = dialogService;

            TemplateService = templateService;

            areasChangedEvent = eventAggregator.GetEvent<AreasChangedEvent>();
            areaModifiedEvent = eventAggregator.GetEvent<AreaModifiedEvent>();
            areasOrderedEvent = eventAggregator.GetEvent<AreasOrderedEvent>();
            areaSelectedEvent = eventAggregator.GetEvent<AreaSelectedEvent>();

            clipSelectedEvent = eventAggregator.GetEvent<SegmentSelectedEvent>();
        }

        #endregion Public Constructors

        #region Public Properties

        public Area Area { get; private set; }

        public List<Area> Areas { get; } = new List<Area>();

        public bool CanUndo => Area?.X1Last.HasValue == true
            && Area.X2Last.HasValue
            && Area.Y1Last.HasValue
            && Area.Y2Last.HasValue;

        public Segment Segment { get; private set; }

        public ITemplateService TemplateService { get; }

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

                area.Segments = area.GetSegments().ToArray();
                area.SetSegments();

                Areas.Add(area);

                scoreboardService.BindArea(
                    area: area,
                    type: area.Type);

                orderDescending = false;
            }
        }

        public void Clear()
        {
            if (Areas.Any())
            {
                for (var index = Areas.Count; index > 0; index--)
                {
                    var area = Areas[index - 1];
                    RemoveArea(area);
                }

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
                var area = GetArea(size);

                Add(area);

                areasChangedEvent.Publish();

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

        public void Next(bool backward)
        {
            var next = Areas.GetNext(
                active: Area,
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
            if (Area != default)
            {
                var canBeRemoved = !Area.HasDimensions;

                if (!canBeRemoved)
                {
                    var messageBoxResult = await dialogService.GetMessageBoxResultAsync(
                        contentMessage: "Shall the selected clip be removed?",
                        contentTitle: "Remove clip");

                    canBeRemoved = messageBoxResult == ButtonResult.Yes;
                }

                if (canBeRemoved)
                {
                    var next = Areas.GetNext(Area);

                    RemoveArea(Area);

                    areasChangedEvent.Publish();

                    Select(next);
                }
            }
        }

        public void Resize(double? left, double? widthMin, double? widthFull, double? widthActual,
            double? top, double? heightMin, double? heightFull, double? heightActual)
        {
            var isModified = false;

            Area.HasDimensions = false;

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
                Area.SetSegments();

                areaModifiedEvent.Publish(Area);
            }
        }

        public void Select(Area area)
        {
            Area = Area != area
                ? area
                : default;

            areaSelectedEvent.Publish(Area);

            if (Segment != default
                && Segment.Area != Area)
            {
                Select();
            }
        }

        public void Select(Segment segment = default)
        {
            Segment = Segment != segment
                ? segment
                : default;

            clipSelectedEvent.Publish(Segment);

            Area = Segment?.Area;

            areaSelectedEvent.Publish(Area);
        }

        public void Undo()
        {
            if (CanUndo)
            {
                var isModified = false;

                isModified |= ResizeVertical(
                    y1: Area.Y1Last,
                    y2: Area.Y2Last);

                isModified |= ResizeHorizontal(
                    x1: Area.X1Last,
                    x2: Area.X2Last);

                if (isModified)
                {
                    Area.SetSegments();

                    areaModifiedEvent.Publish(Area);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Area GetArea(int size)
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

            if (Area != default)
            {
                result.NoiseRemoval = Area.NoiseRemoval;
                result.ThresholdMonochrome = Area.ThresholdMonochrome;
            }

            return result;
        }

        private void RemoveArea(Area area)
        {
            if (area != default)
            {
                scoreboardService.ReleaseArea(area);

                Areas.Remove(area);
            }
        }

        private bool ResizeHorizontal(double? x1, double? x2)
        {
            var result = false;

            if (x1.HasValue
                && x2.HasValue)
            {
                if (Area.X1 != x1 || Area.X2 != x2)
                {
                    Area.X1Last = Area.X1;
                    Area.X2Last = Area.X2;

                    Area.X1 = x1.Value;
                    Area.X2 = x2.Value;

                    result = true;
                }

                Area.HasDimensions = true;
            }

            return result;
        }

        private bool ResizeVertical(double? y1, double? y2)
        {
            var result = false;

            if (y1.HasValue
                && y2.HasValue)
            {
                if (Area.Y1 != y1 || Area.Y2 != y2)
                {
                    Area.Y1Last = Area.Y1;
                    Area.Y2Last = Area.Y2;

                    Area.Y1 = y1.Value;
                    Area.Y2 = y2.Value;

                    result = true;
                }

                Area.HasDimensions = true;
            }

            return result;
        }

        #endregion Private Methods
    }
}