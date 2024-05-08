using Score2Stream.Commons.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.AreaService.Extensions
{
    internal static class AreaExtensions
    {
        #region Public Methods

        public static IEnumerable<Segment> GetSegments(this Area area)
        {
            for (var position = 0; position < area.Size; position++)
            {
                var result = new Segment
                {
                    Area = area,
                    Position = position,
                };

                yield return result;
            }
        }

        public static void SetSegments(this Area area)
        {
            if (area.Segments?.Any() == true)
            {
                var width = (area.X2 - area.X1) / (double)area.Segments.Count();

                var index = 0;

                foreach (var segment in area.Segments)
                {
                    segment.X1 = area.X1 + (width * index);

                    segment.X2 = segment != area.Segments.Last()
                        ? area.X1 + (width * ++index)
                        : area.X2;
                }
            }
        }

        #endregion Public Methods
    }
}