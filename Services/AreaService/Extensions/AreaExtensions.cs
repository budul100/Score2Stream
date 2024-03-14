using System.Collections.Generic;
using System.Linq;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.AreaService.Extensions
{
    internal static class AreaExtensions
    {
        #region Public Methods

        public static IEnumerable<Segment> GetClips(this Area area)
        {
            for (var index = 0; index < area.Size; index++)
            {
                var result = new Segment
                {
                    Area = area,
                    Index = index,
                };

                yield return result;
            }
        }

        public static void SetClips(this Area area)
        {
            if (area.Segments?.Any() == true)
            {
                var width = (area.X2 - area.X1) / (double)area.Segments.Count();

                var index = 0;

                foreach (var clip in area.Segments)
                {
                    clip.X1 = area.X1 + (width * index);
                    clip.X2 = clip != area.Segments.Last()
                        ? area.X1 + (width * ++index)
                        : area.X2;
                }
            }
        }

        #endregion Public Methods
    }
}