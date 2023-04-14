using Avalonia.Media;

namespace Score2Stream.ScoreboardService.Extensions
{
    internal static class ColorExtensions
    {
        #region Public Methods

        public static string GetColorHex(this Color value)
        {
            var result = $"#{value.R:X2}{value.G:X2}{value.B:X2}";

            return result;
        }

        #endregion Public Methods
    }
}