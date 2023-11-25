namespace Score2Stream.Commons.Models.Settings
{
    public class App
    {
        #region Public Properties

        public int Height { get; set; } = Constants.AppHeightDefault;

        public int Width { get; set; } = Constants.AppWidthDefault;

        public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

        #endregion Public Properties
    }
}