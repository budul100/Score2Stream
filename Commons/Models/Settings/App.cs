using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class App
    {
        #region Public Properties

        public bool AllowMultipleInstances { get; set; }

        public int Height { get; set; } = Constants.AppSizeHeightDefault;

        public int Width { get; set; } = Constants.AppSizeWidthDefault;

        public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

        #endregion Public Properties
    }
}