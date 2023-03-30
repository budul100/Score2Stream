using Prism.Mvvm;

namespace ScoreboardOCR.ViewModels
{
    public class MainViewModel : BindableBase
    {
        #region Private Fields

        private int height;
        private string title = "ScoreboardOCR";
        private int width;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel()
        {
            Height = 800;
            Width = 1200;
        }

        #endregion Public Constructors

        #region Public Properties

        public int Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

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
    }
}