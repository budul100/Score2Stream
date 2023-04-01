using Core.Events;
using Core.Models.Receiver;
using MvvmValidation;
using Prism.Commands;
using Prism.Events;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Mvvm;
using System.Collections.ObjectModel;
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
        private readonly ITemplateService templateService;
        private bool isActive;
        private string name;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(Clip clip, IClipService clipService, ITemplateService templateService,
            IEventAggregator eventAggregator)
        {
            this.clip = clip;
            this.templateService = templateService;
            this.eventAggregator = eventAggregator;

            Name = clip.Name;

            var selectEvent = eventAggregator.GetEvent<SelectClipEvent>();
            OnClickCommand = new DelegateCommand(
                executeMethod: () => selectEvent.Publish(clip));

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => IsActive = c == clip,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: () => OnUpdateTemplates(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<WebcamUpdatedEvent>().Subscribe(
                action: () => OnUpdateWebcam(),
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
                validateDelegate: () => RuleResult.Assert(Name == this.clip.Name || clipService.IsUniqueName(Name),
                errorMessage: $"Name {Name} is already used. Please choose another one."));

            OnUpdateTemplates();
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Content => clip.Content;

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

        public Template Template
        {
            get { return clip.Template; }
            set
            {
                clip.Template = value;
                RaisePropertyChanged(nameof(Template));
            }
        }

        public ObservableCollection<Template> Templates { get; private set; }

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

        public string Value => !string.IsNullOrWhiteSpace(clip.Value)
            ? $"=> {clip.Value}"
            : default;

        #endregion Public Properties

        #region Private Methods

        private void OnUpdateTemplates()
        {
            Templates = new ObservableCollection<Template>(templateService.Templates);

            RaisePropertyChanged(nameof(Templates));
            RaisePropertyChanged(nameof(Template));
        }

        private void OnUpdateWebcam()
        {
            RaisePropertyChanged(nameof(Content));
            RaisePropertyChanged(nameof(Value));
        }

        #endregion Private Methods
    }
}