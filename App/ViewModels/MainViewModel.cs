using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Core;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Settings;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Score2Stream.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly string assemblyTitle;
        private readonly IInputService inputService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IScoreboardService scoreboardService;
        private readonly Session settings;
        private readonly ISettingsService<Session> settingsService;

        private DateTime? lastUpdateTitle;
        private string title = "Score2Stream";
        private bool userWantsToQuit;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(ISettingsService<Session> settingsService, IInputService inputService,
            IScoreboardService scoreboardService, IMessageBoxService messageBoxService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.inputService = inputService;
            this.scoreboardService = scoreboardService;
            this.messageBoxService = messageBoxService;

            settings = settingsService.Get();
            assemblyTitle = GetAssemblyTitle();

            this.OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosingAsync);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => UpdateTitle(),
                keepSubscriberReferenceAlive: true);

            UpdateTitle();
        }

        #endregion Public Constructors

        #region Public Properties

        public int Height
        {
            get
            {
                return settings.App.Height;
            }
            set
            {
                if (settings.App.Height != value)
                {
                    settings.App.Height = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(Height));
                }
            }
        }

        public DelegateCommand<CancelEventArgs> OnClosingCommand { get; }

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public int Width
        {
            get
            {
                return settings.App.Width;
            }
            set
            {
                if (settings.App.Width != value)
                {
                    settings.App.Width = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(Width));
                }
            }
        }

        public WindowState WindowState
        {
            get
            {
                return Enum.Parse<WindowState>(settings.App.WindowState);
            }
            set
            {
                if (settings.App.WindowState != value.ToString())
                {
                    settings.App.WindowState = value.ToString();

                    settingsService.Save();

                    RaisePropertyChanged(nameof(WindowState));
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

        private static string GetAssemblyTitle()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();

            var version = new StringBuilder();

            version.Append(assembly.Version.Major);

            version.Append(Constants.SplitterVersion);
            version.Append(assembly.Version.Minor);

            if (version.Length > 0
                && assembly.Version.Build > 0)
            {
                version.Append(Constants.SplitterVersion);
                version.Append(assembly.Version.Build);
            }

            var result = $"{nameof(Score2Stream)} ({version})";

            return result;
        }

        private string GetProcessingTimes()
        {
            if (inputService.Inputs?.Any() == true)
            {
                var result = new StringBuilder();

                var videoServices = inputService.Inputs
                    .Select(i => i.VideoService)
                    .Where(v => v?.ProcessingTime != default).ToArray();

                foreach (var videoService in videoServices)
                {
                    if (result.Length > 0)
                    {
                        result.Append(Constants.SplitterTitle);
                    }

                    result
                        .Append(videoService.Name)
                        .Append(": ")
                        .Append(videoService.ProcessingTime.Value.TotalMilliseconds.ToString("0"))
                        .Append("ms");
                }

                return result.ToString();
            }

            return default;
        }

        private string GetTitle()
        {
            var result = new StringBuilder();

            result.Append(assemblyTitle);

            var processingTime = GetProcessingTimes();

            if (!string.IsNullOrWhiteSpace(processingTime))
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append(processingTime);
            }

            if (!string.IsNullOrWhiteSpace(scoreboardService.ClockGame))
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append("Game: ").Append(scoreboardService.ClockGame);
            }

            if (!string.IsNullOrWhiteSpace(scoreboardService.ClockShot))
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append("Shot: ").Append(scoreboardService.ClockShot);
            }

            if (!string.IsNullOrWhiteSpace(scoreboardService.Period))
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append("Period: ").Append(scoreboardService.Period);
            }

            if (!string.IsNullOrWhiteSpace(scoreboardService.ScoreHome))
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append("Home: ").Append(scoreboardService.ScoreHome);
            }

            if (!string.IsNullOrWhiteSpace(scoreboardService.ScoreGuest))
            {
                if (result.Length > 0)
                {
                    result.Append(Constants.SplitterTitle);
                }

                result.Append("Guest: ").Append(scoreboardService.ScoreGuest);
            }

            return result.ToString();
        }

        private async void OnClosingAsync(CancelEventArgs eventArgs)
        {
            userWantsToQuit = userWantsToQuit || !inputService.IsActive;

            if (!userWantsToQuit
                && App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                eventArgs.Cancel = true;

                var result = await messageBoxService.GetMessageBoxResultAsync(
                    contentMessage: "Shall the application be closed?",
                    contentTitle: "Close application");

                if (result == ButtonResult.Yes)
                {
                    eventArgs.Cancel = false;

                    userWantsToQuit = true;
                    desktop.Shutdown();
                }
            }
        }

        private void UpdateTitle()
        {
            if (!lastUpdateTitle.HasValue
                || lastUpdateTitle.Value.AddMilliseconds(Constants.DurationUpdateTitle) < DateTime.Now)
            {
                Title = GetTitle();
                lastUpdateTitle = DateTime.Now;
            }
        }

        #endregion Private Methods
    }
}