using Score2Stream.Commons.Assets;

namespace Score2Stream.Commons.Models.Settings
{
    public class App
    {
        #region Public Properties

        public bool AllowMultipleInstances { get; set; }

        public int Height { get; set; } = Defaults.AppSizeHeight;

        public int Width { get; set; } = Defaults.AppSizeWidth;

        public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

        #endregion Public Properties
    }
}