using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Score2Stream.Commons.Logging
{
    public sealed partial class FileErrorLoggerProvider(string filePath)
        : ILoggerProvider
    {
        #region Private Fields

        private readonly object writeLock = new();

        private bool isDisposed;
        private StreamWriter writer;

        #endregion Private Fields

        #region Public Methods

        public ILogger CreateLogger(string categoryName)
        {
            var result = new FileErrorLogger(
                categoryName: categoryName,
                provider: this);

            return result;
        }

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        #endregion Public Methods

        #region Internal Methods

        internal static bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        internal void Write(string categoryName, LogLevel logLevel, string message,
            Exception exception)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            try
            {
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | " +
                    $"{logLevel} | {categoryName} | {message}";

                if (exception != default)
                {
                    logMessage = $"{logMessage}{Environment.NewLine}{exception}";
                }

                lock (writeLock)
                {
                    EnsureWriter();

                    writer?.WriteLine(logMessage);
                }
            }
            catch
            { }
        }

        #endregion Internal Methods

        #region Private Methods

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    lock (writeLock)
                    {
                        writer?.Dispose();
                        writer = default;
                    }
                }

                isDisposed = true;
            }
        }

        private void EnsureWriter()
        {
            if (writer != default)
            {
                return;
            }

            var folderPath = System.IO.Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(folderPath)
                && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            writer = new StreamWriter(new FileStream(
                path: filePath,
                mode: FileMode.Append,
                access: FileAccess.Write,
                share: FileShare.Read))
            {
                AutoFlush = true
            };
        }

        #endregion Private Methods

        #region Private Classes

        private sealed class FileErrorLogger(string categoryName, FileErrorLoggerProvider provider)
            : ILogger
        {
            #region Public Methods

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => FileErrorLoggerProvider.IsEnabled(logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter != default
                    ? formatter(state, exception)
                    : state?.ToString();

                provider.Write(
                    categoryName: categoryName,
                    logLevel: logLevel,
                    message: message ?? string.Empty,
                    exception: exception);
            }

            #endregion Public Methods
        }

        private sealed class NullScope
            : IDisposable
        {
            #region Public Fields

            public static readonly NullScope Instance = new();

            #endregion Public Fields

            #region Public Methods

            public void Dispose()
            { }

            #endregion Public Methods
        }

        #endregion Private Classes
    }
}