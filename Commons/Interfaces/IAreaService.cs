using System.Collections.Generic;
using System.Threading.Tasks;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Interfaces
{
    public interface IAreaService
    {
        #region Public Properties

        Area Active { get; }

        IReadOnlyList<Area> Areas { get; }

        bool CanUndo { get; }

        Segment ActiveSegment { get; }

        #endregion Public Properties

        #region Public Methods

        void Add(Area area);

        void Clear();

        Task ClearAsync();

        void Create(int size);

        void Initialize(Input input);

        void Next(bool backward);

        void Order(bool reverseOrder = false);

        Task RemoveAsync();

        void Resize(double? left, double? widthMin, double? widthFull, double? widthActual,
            double? top, double? heightMin, double? heightFull, double? heightActual);

        void Select(Area area);

        void Select(Segment clip = default);

        void Undo();

        #endregion Public Methods
    }
}