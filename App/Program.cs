using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.ReactiveUI;

namespace Score2Stream.App
{
    internal static class Program
    {
        #region Private Fields

        private const int MaxGpuResourceSizeBytes = 256000000;

        #endregion Private Fields

        #region Public Methods

        public static AppBuilder BuildAvaloniaApp() => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = MaxGpuResourceSizeBytes
            })
            .With(new X11PlatformOptions
            {
                EnableMultiTouch = true,
                UseDBusMenu = true
            })
            .With(new Win32PlatformOptions())
            .UseSkia()
            .UseReactiveUI()
            .UseManagedSystemDialogs()
            .LogToTrace();

        #endregion Public Methods

        #region Private Methods

        private static double GetScaling(string[] args)
        {
            var idx = Array.IndexOf(args, "--scaling");

            if (idx != 0 && args.Length > idx + 1 &&
                double.TryParse(
                    s: args[idx + 1],
                    style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture,
                    result: out var scaling))
            {
                return scaling;
            }

            return 1;
        }

        private static int Main(string[] args)
        {
            var builder = BuildAvaloniaApp();

            if (args.Contains("--fbdev"))
            {
                SilenceConsole();

                var scaling = GetScaling(args);

                return builder.StartLinuxFbDev(
                    args: args,
                    scaling: scaling);
            }
            else if (args.Contains("--drm"))
            {
                SilenceConsole();

                var scaling = GetScaling(args);

                return builder.StartLinuxDrm(
                    args: args,
                    scaling: scaling);
            }
            else
            {
                return builder.StartWithClassicDesktopLifetime(
                    args: args);
            }
        }

        private static void SilenceConsole()
        {
            new Thread(() =>
            {
                Console.CursorVisible = false;

                while (true) Console.ReadKey(true);
            })
            {
                IsBackground = true
            }.Start();
        }

        #endregion Private Methods
    }
}