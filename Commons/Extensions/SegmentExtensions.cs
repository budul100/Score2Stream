using System.Text;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Extensions
{
    public static class SegmentExtensions
    {
        #region Public Methods

        public static string GetDescription(this Segment segment, bool showEmptyValue, bool includeType)
        {
            var result = new StringBuilder();

            if (segment != default)
            {
                if (includeType)
                {
                    if (segment.Type != SegmentType.None)
                    {
                        result.Append($"{segment.Type.GetDescription()} => ");
                    }
                    else
                    {
                        result.Append($"{segment.Area.Name} => ");
                    }
                }

                if (segment.HasValue)
                {
                    result.Append(segment.Value);
                }
                else if (showEmptyValue
                    && segment.Area.Template?.Empty != default)
                {
                    result.Append(segment.Area.Template?.Empty);
                }
                else
                {
                    result.Append("-/-");
                }

                if (segment.HasValue)
                {
                    if (result.Length > 0)
                    {
                        result.Append(' ');
                    }

                    result.Append($"({segment.Similarity}%)");
                }
            }

            return result.ToString();
        }

        #endregion Public Methods
    }
}