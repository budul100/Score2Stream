using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Extensions
{
    public static class ClipExtensions
    {
        #region Public Methods

        public static string GetDescription(this Segment clip, bool includeType = false)
        {
            var result = default(string);

            if (clip != default)
            {
                string value;

                if (clip.Area.Template != default)
                {
                    value = !string.IsNullOrWhiteSpace(clip.Value)
                        ? $"{clip.Value} ({clip.Similarity}%)"
                        : $"-/- ({clip.Similarity}%)";
                }
                else
                {
                    value = "-/-";
                }

                if (includeType)
                {
                    result = clip.Type != ClipType.None
                        ? $"{clip.Type.GetDescription()} => {value}"
                        : $"{clip.Area.Name} => {value}";
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