using MvvmValidation;
using Prism.Commands;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;
using System;
using System.Windows.Media.Imaging;

namespace ClipModule.ViewModels
{
    public class ClipViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private BitmapSource content;
        private bool isActive;
        private string name;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(Clip clip)
        {
            this.Clip = clip;

            OnClickCommand = new DelegateCommand(OnClick);

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(!string.IsNullOrEmpty(Name), "Name is required"));
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler OnClipSelectedEvent;

        #endregion Public Events

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
                if (!string.IsNullOrWhiteSpace(value)
                    && Clip.Name != value)
                {
                    Clip.Name = value;
                }

                SetProperty(ref name, value);
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
            OnClipSelectedEvent?.Invoke(
                sender: this,
                e: default);
        }

        #endregion Private Methods
    }
}