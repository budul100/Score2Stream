using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Mvvm;
using Score2Stream.Core.Interfaces;
using System.ComponentModel;

namespace Score2Stream.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IInputService inputService;
        private readonly IMessageBoxService messageBoxService;

        private int height;
        private string title = "Score2Stream";
        private int width;
        private bool userWantsToQuit;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IInputService inputService, IMessageBoxService messageBoxService)
        {
            this.inputService = inputService;
            this.messageBoxService = messageBoxService;

            Height = 800;
            Width = 1200;

            this.OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosingAsync);
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
    }

    #endregion Private Methods
}