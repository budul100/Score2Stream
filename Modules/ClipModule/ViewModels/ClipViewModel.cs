using Core.Events;
using MvvmValidation;
using Prism.Commands;
using Prism.Events;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;
using System;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace ClipModule.ViewModels
{
    public class ClipViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly Clip clip;
        private readonly IEventAggregator eventAggregator;

        private BitmapSource content;
        private bool isActive;
        private string name;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(Clip clip, Func<string, bool> uniqueNameGetter, IEventAggregator eventAggregator)
        {
            this.clip = clip;
            this.eventAggregator = eventAggregator;

            Name = clip.Name;
            OnClickCommand = new DelegateCommand(OnClick);

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: (c) => IsActive = c == clip,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ContentUpdatedEvent>().Subscribe(
                action: () => Content = clip.Content,
                keepSubscriberReferenceAlive: true);

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(!string.IsNullOrEmpty(Name),
                errorMessage: "Name is required."));

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(Regex.IsMatch(Name, "^\\S+$"),
                errorMessage: "Name cannot contain whitespaces."));

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(Name == this.clip.Name || uniqueNameGetter?.Invoke(Name) != false,
                errorMessage: $"Name {Name} is already used. Please choose another one."));
        }

        #endregion Public Constructors

        #region Public Properties

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
                    && this.clip.Name != value)
                {
                    this.clip.Name = value;

                    eventAggregator
                        .GetEvent<ClipUpdatedEvent>()
                        .Publish(clip);
                }
            }
        }

        public DelegateCommand OnClickCommand { get; }

        public int ThresholdMonochrome
        {
            get { return this.clip.ThresholdMonochrome; }
            set
            {
                if (value >= 0
                    && value <= 100
                    && this.clip.ThresholdMonochrome != value)
                {
                    this.clip.ThresholdMonochrome = value;

                    eventAggregator
                        .GetEvent<ClipUpdatedEvent>()
                        .Publish(clip);
                }

                RaisePropertyChanged(nameof(ThresholdMonochrome));
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void OnClick()
        {
            eventAggregator.GetEvent<SelectClipEvent>().Publish(clip);
        }

        #endregion Private Methods
    }
}