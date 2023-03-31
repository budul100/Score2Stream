using MvvmValidation;
using Prism.Commands;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace ClipModule.ViewModels
{
    public class ClipViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly IClipService clipService;
        private BitmapSource content;
        private bool isActive;
        private string name;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(IClipService clipService, Clip clip)
        {
            this.clipService = clipService;

            Clip = clip;
            Name = clip.Name;

            OnClickCommand = new DelegateCommand(OnClick);

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(!string.IsNullOrEmpty(Name), "Name is required."));

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(Regex.IsMatch(Name, "\\S+"), "Name cannot contain whitespaces."));

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(clipService.IsUniqueName(Name), $"Name {Name} is already used. Please choose another one."));
        }

        #endregion Public Constructors

        #region Public Properties

        public Clip Clip { get; }

        public BitmapSource Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public string Name
        {
            get { return name; }
            set
            {
                SetProperty(ref name, value);
                var validation = Validator.Validate(nameof(Name));

                if (validation.IsValid
                    && Clip.Name != value)
                {
                    Clip.Name = value;
                }
            }
        }

        public DelegateCommand OnClickCommand { get; }

        public int ThresholdMonochrome
        {
            get { return Clip.ThresholdMonochrome; }
            set
            {
                if (value >= 0
                    && value <= 100
                    && Clip.ThresholdMonochrome != value)
                {
                    Clip.ThresholdMonochrome = value;
                }

                RaisePropertyChanged(nameof(ThresholdMonochrome));
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Update()
        {
            Content = Clip.Content;
        }

        #endregion Public Methods

        #region Private Methods

        private void OnClick()
        {
            clipService.Activate(Clip);
        }

        #endregion Private Methods
    }
}