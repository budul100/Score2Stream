using System;
using Avalonia;

namespace TestApp
{
    internal static class Program
    {
        #region Public Methods

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        #endregion Public Methods
    }
}