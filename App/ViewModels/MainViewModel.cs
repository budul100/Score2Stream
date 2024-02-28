using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Video;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Settings;

namespace Score2Stream.App.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly string assemblyTitle;
        private readonly IDialogService dialogService;
        private readonly IInputService inputService;
        private readonly IScoreboardService scoreboardService;
        private readonly ISettingsService<Session> settingsService;

        private DateTime? lastUpdateTitle;
        private string title;
        private bool userWantsToQuit;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(ISettingsService<Session> settingsService, IInputService inputService,
            IScoreboardService scoreboardService, IDialogService dialogService, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.inputService = inputService;
            this.scoreboardService = scoreboardService;
            this.dialogService = dialogService;

            OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosingAsync);

            assemblyTitle = GetAssemblyTitle();

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => UpdateTitle(),
                keepSubscriberReferenceAlive: true);

            LoadSettings();

            UpdateTitle();
        }

        #endregion Public Constructors

        #region Public Properties

        public int? Height
        {
            get
            {
                return settingsService.Contents?.App.Height;
            }
            set
            {
                if (value.HasValue
                    && settingsService.Contents.App.Height != value)
                {
                    settingsService.Contents.App.Height = value.Value;

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

        public int? Width
        {
            get
            {
                return settingsService.Contents?.App.Width;
            }
            set
            {
                if (value.HasValue
                    && settingsService.Contents.App.Width != value)
                {
                    settingsService.Contents.App.Width = value.Value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(Width));
                }
            }
        }

        public WindowState? WindowState
        {
            get
            {
                if (Enum.TryParse<WindowState>(
                    value: settingsService.Contents?.App.WindowState,
                    result: out var result))
                {
                    return result;
                }
                else
                {
                    return default;
                }
            }
            set
            {
                if (settingsService.Contents.App.WindowState != value.ToString())
                {
                    settingsService.Contents.App.WindowState = value.ToString();

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

            var result = $"{nameof(Score2Stream)} {version}";

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

        private void LoadSettings()
        {
            var sessionPath = settingsService.GetPath(
                appName: Texts.AppName,
                fileName: Defaults.FileNameSettingsDefault);

            settingsService.Load(sessionPath);
        }

        private async void OnClosingAsync(CancelEventArgs eventArgs)
        {
            userWantsToQuit = userWantsToQuit || !inputService.IsActive;

            if (!userWantsToQuit
                && App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                eventArgs.Cancel = true;

                var result = await dialogService.GetMessageBoxResultAsync(
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