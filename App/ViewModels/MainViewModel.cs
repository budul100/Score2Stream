using Prism.Mvvm;

namespace ScoreboardOCR.ViewModels
{
    public class MainViewModel : BindableBase
    {
        #region Private Fields

        private string _title = "Prism Application";

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel()
        {
        }

        #endregion Public Constructors

        #region Public Properties

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        #endregion Public Properties
    }
}