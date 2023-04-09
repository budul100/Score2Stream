using MvvmDialogs;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows;

namespace ScoreboardOCR.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IDialogService dialogService;
        private int height;
        private string title = "ScoreboardOCR";
        private int width;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            Height = 800;
            Width = 1200;

            this.OnClosingCommand = new DelegateCommand<CancelEventArgs>(OnClosing);
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

        private void OnClosing(CancelEventArgs eventArgs)
        {
            var result = dialogService.ShowMessageBox(
                ownerViewModel: this,
                messageBoxText: "Shall the application be closed?",
                caption: "Close application",
                button: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Question,
                defaultResult: MessageBoxResult.No);

            eventArgs.Cancel = result == MessageBoxResult.No;
        }

        #endregion Private Methods
    }
}