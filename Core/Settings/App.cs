namespace Score2Stream.Core.Settings
{
    public class App
    {
        #region Private Fields

        private const int HeightDefault = 800;
        private const int WidthDefault = 1200;

        #endregion Private Fields

        #region Public Properties

        public int Height { get; set; } = HeightDefault;

        public int Width { get; set; } = WidthDefault;

        public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

        #endregion Public Properties
    }
}