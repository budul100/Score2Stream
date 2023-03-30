using MvvmValidation;
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
        private string name;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(Clip clip)
        {
            this.clip = clip;

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(!string.IsNullOrEmpty(Name), "Name is required"));
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
            get { return name; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value)
                    && clip.Name != value)
                {
                    clip.Name = value;
                }

                SetProperty(ref name, value);
            }
        }

        public int ThresholdMonochrome
        {
            get { return clip.ThresholdMonochrome; }
            set
            {
                if (value >= 0
                    && value <= 100
                    && clip.ThresholdMonochrome != value)
                {
                    clip.ThresholdMonochrome = value;
                }

                RaisePropertyChanged(nameof(ThresholdMonochrome));
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Update()
        {
            Content = clip.Content;
        }

        #endregion Public Methods
    }
}