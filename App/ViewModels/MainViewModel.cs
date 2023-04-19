using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
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

        private const int PeriodUpdateTitle = 500;
        private const char SplitterProcessingTime = '/';
        private const char SplitterVersion = '.';

        private readonly string assemblyTitle;
        private readonly IInputService inputService;
        private readonly IMessageBoxService messageBoxService;

        private int height;
        private DateTime? lastUpdateTitle;
        private string title = "Score2Stream";
        private bool userWantsToQuit;
        private int width;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IInputService inputService, IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator)
        {
            this.inputService = inputService;
            this.messageBoxService = messageBoxService;

            assemblyTitle = GetAssemblyTitle();

            Height = 800;
            Width = 1200;

            this.OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosingAsync);

            eventAggregator.GetEvent<VideoUpdatedEvent>()
                .Subscribe(() => UpdateTitle());

            UpdateTitle();
        }

        #endregion Public Constructors

        #region Public Properties

        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        public DelegateCommand<CancelEventArgs> OnClosingCommand { get; }

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public int Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private static string GetAssemblyTitle()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();

            var version = new StringBuilder();

            version.Append(assembly.Version.Major);

            version.Append(SplitterVersion);
            version.Append(assembly.Version.Minor);

            if (version.Length > 0
                && assembly.Version.Build > 0)
            {
                version.Append(SplitterVersion);
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
                        result.Append(SplitterProcessingTime);
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
            var result = Title;

            if (!lastUpdateTitle.HasValue
                || lastUpdateTitle.Value.AddMilliseconds(PeriodUpdateTitle) < DateTime.Now)
            {
                var processingTime = GetProcessingTimes();

                result = !string.IsNullOrWhiteSpace(processingTime)
                    ? $"{assemblyTitle} | {processingTime}"
                    : assemblyTitle;

                lastUpdateTitle = DateTime.Now;
            }

            return result;
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
            Title = GetTitle();
        }

        #endregion Private Methods
    }
}