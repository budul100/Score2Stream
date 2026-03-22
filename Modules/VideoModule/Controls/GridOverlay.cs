using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Score2Stream.VideoModule.Controls
{
    public class GridOverlay
        : Control
    {
        #region Private Fields

        private const int GridLength = 5;

        private static readonly SolidColorBrush GridBrush = new SolidColorBrush(
            color: Colors.White,
            opacity: 0.2);

        private static readonly Pen GridPen = new(
            brush: GridBrush,
            thickness: 1);

        #endregion Private Fields

        #region Public Methods

        public override void Render(DrawingContext context)
        {
            for (var i = 1; i <= GridLength; i++)
            {
                var x = Bounds.Width * i / 6;
                var y = Bounds.Height * i / 6;

                context.DrawLine(
                    pen: GridPen,
                    p1: new Point(x, 0),
                    p2: new Point(x, Bounds.Height));

                context.DrawLine(
                    pen: GridPen,
                    p1: new Point(0, y),
                    p2: new Point(Bounds.Width, y));
            }
        }

        #endregion Public Methods
    }
}