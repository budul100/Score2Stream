using OpenCvSharp;
using System;

namespace Score2Stream.VideoService.Extensions
{
    internal static class GeometryExtensions
    {
        #region Public Methods

        public static Rect? GetRectangle(this Size picSize, int firstX, int firstY, int secondX, int secondY,
            Size? vizSize = default)
        {
            var result = default(Rect?);

            vizSize ??= picSize;

            var firstPoint = new Point(firstX, firstY);
            var secondPoint = new Point(secondX, secondY);

            var firstScaled = firstPoint.Scaled(
                picSize: picSize,
                vizSize: vizSize.Value);

            var secondScaled = secondPoint.Scaled(
                picSize: picSize,
                vizSize: vizSize.Value);

            if (firstScaled.HasValue
                && secondScaled.HasValue)
            {
                result = new Rect(
                    x: Math.Min(firstScaled.Value.X, secondScaled.Value.X),
                    y: Math.Min(firstScaled.Value.Y, secondScaled.Value.Y),
                    width: Math.Abs(firstScaled.Value.X - secondScaled.Value.X),
                    height: Math.Abs(firstScaled.Value.Y - secondScaled.Value.Y));
            }

            return result;
        }

        public static Point? Scaled(this Point point, Size picSize, Size vizSize)
        {
            try
            {
                if (picSize == vizSize)
                {
                    return point;
                }
                else if (vizSize.Height > vizSize.Width)
                {
                    var scale = (double)vizSize.Height / picSize.Height;
                    var spare = (picSize.Width - (vizSize.Width / scale)) / 2;

                    var x = point.X - spare;
                    x = x < 0 ? 0 : x;

                    return new Point(
                        (int)(x * scale),
                        (int)(point.Y * scale));
                }
                else
                {
                    var scale = (double)vizSize.Width / picSize.Width;
                    var spare = (picSize.Height - (vizSize.Height / scale)) / 2;

                    var y = point.Y - spare;
                    y = y < 0 ? 0 : y;

                    return new Point(
                        (int)(point.X * scale),
                        (int)(y * scale));
                }
            }
            catch
            {
                return default;
            }
        }

        #endregion Public Methods
    }
}