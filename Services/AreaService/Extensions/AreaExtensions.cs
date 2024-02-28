using System.Collections.Generic;
using System.Linq;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.AreaService.Extensions
{
    internal static class AreaExtensions
    {
        #region Public Methods

        public static IEnumerable<Clip> GetClips(this Area area)
        {
            for (var index = 0; index < area.Size; index++)
            {
                var result = new Clip
                {
                    Area = area,
                    Index = index,
                };

                yield return result;
            }
        }

        public static void SetClips(this Area area)
        {
            if (area.Clips?.Any() == true)
            {
                var width = (area.X2 - area.X1) / (double)area.Clips.Count();

                var index = 0;

                foreach (var clip in area.Clips)
                {
                    clip.X1 = area.X1 + (width * index);
                    clip.X2 = clip != area.Clips.Last()
                        ? area.X1 + (width * ++index)
                        : area.X2;
                }
            }
        }

        #endregion Public Methods
    }
}