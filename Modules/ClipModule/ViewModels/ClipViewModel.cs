using MvvmValidation;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;
using System.Windows.Media.Imaging;

namespace ClipModule.ViewModels
{
    public class ClipViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly Clip clip;
        private BitmapSource content;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(Clip clip, IWebcamService webcamService)
        {
            this.clip = clip;

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(!string.IsNullOrEmpty(Name), "Name is required"));

            webcamService.OnContentChangedEvent += OnContentChanged;
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        public string Name
        {
            get { return clip.Name; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value)
                    && clip.Name != value)
                {
                    clip.Name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void OnContentChanged(object sender, System.EventArgs e)
        {
            Content = clip.Content;
        }

        private void OnWebcamChanged()
        {
            Content = clip.Content;
        }

        #endregion Private Methods
    }
}