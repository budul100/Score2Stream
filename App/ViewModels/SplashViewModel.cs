using Prism.Commands;
using Prism.Mvvm;
using Score2Stream.Commons.Assets;
using System.Threading;

namespace Score2Stream.App.ViewModels
{
    public class SplashViewModel
        : BindableBase
    {
        #region Public Fields

        public DelegateCommand CancelCommand;

        #endregion Public Fields

        #region Private Fields

        private readonly CancellationTokenSource cancellationTokenSource = new();

        private string message;

        #endregion Private Fields

        #region Public Constructors

        public SplashViewModel()
        {
            CancelCommand = new DelegateCommand(Cancel);

            Message = Texts.SplashLoadingMessage;
        }

        #endregion Public Constructors

        #region Public Properties

        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        public string Message
        {
            get { return message; }
            set { SetProperty(ref message, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Cancel()
        {
            Message = Texts.SplashCancellingMessage;

            cancellationTokenSource.Cancel();
        }

        #endregion Public Methods
    }
}