using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Mvvm;
using Score2Stream.Core.Interfaces;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Score2Stream.ViewModels
{
    public class MainViewModel
        : BindableBase, IMessageBoxService
    {
        #region Private Fields

        private int height;
        private string title = "Score2Stream";
        private int width;
        private bool UserWantsToQuit;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel()
        {
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

        public async Task<ButtonResult> GetMessageBoxResultAsync(MessageBoxStandardParams messageBoxParams)
        {
            var result = default(ButtonResult);

            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dialog = MessageBoxManager.GetMessageBoxStandardWindow(messageBoxParams);

                result = await dialog.ShowDialog(desktop.MainWindow);
            }

            return result;
        }

        #endregion Public Properties

        #region Private Methods

        private async void OnClosingAsync(CancelEventArgs eventArgs)
        {
            if (!UserWantsToQuit
                && App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                eventArgs.Cancel = true;

                var messageBoxParams = new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentMessage = "Shall the application be closed?",
                    ContentTitle = "Close application",
                    EnterDefaultButton = ClickEnum.Yes,
                    EscDefaultButton = ClickEnum.No,
                    Icon = Icon.Question,
                    ShowInCenter = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                var result = await GetMessageBoxResultAsync(messageBoxParams);

                if (result == ButtonResult.Yes)
                {
                    eventArgs.Cancel = false;

                    UserWantsToQuit = true;
                    desktop.Shutdown();
                }
            }
        }
    }

    #endregion Private Methods
}