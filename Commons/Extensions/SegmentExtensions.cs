using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Extensions
{
    public static class SegmentExtensions
    {
        #region Public Methods

        public static string GetDescription(this Segment segment, bool includeType = false)
        {
            var result = default(string);

            if (segment != default)
            {
                string value;

                if (segment.Area.Template != default)
                {
                    value = !string.IsNullOrWhiteSpace(segment.Value)
                        ? $"{segment.Value} ({segment.Similarity}%)"
                        : $"-/- ({segment.Similarity}%)";
                }
                else
                {
                    value = "-/-";
                }

                if (includeType)
                {
                    result = segment.Type != SegmentType.None
                        ? $"{segment.Type.GetDescription()} => {value}"
                        : $"{segment.Area.Name} => {value}";
                }
                else
                {
                    result = value;
                }
            }

            return result;
        }

        #endregion Public Methods
    }
}