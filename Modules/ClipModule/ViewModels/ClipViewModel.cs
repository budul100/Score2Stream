using Core.Events.Clips;
using Core.Events.Templates;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using Core.Prism;
using MvvmValidation;
using Prism.Commands;
using Prism.Events;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace ClipModule.ViewModels
{
    public class ClipViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private Clip clip;
        private IClipService clipService;
        private bool isActive;
        private string name;

        #endregion Private Fields

        #region Public Constructors

        public ClipViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            OnClickCommand = new DelegateCommand(
                executeMethod: () => clipService?.Select(clip));

            eventAggregator.GetEvent<ClipSelectedEvent>().Subscribe(
                action: c => IsActive = c == clip,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnTemplateSelected(),
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: () => UpdateTemplates(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => UpdateBitmap(),
                keepSubscriberReferenceAlive: true);

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(!string.IsNullOrEmpty(Name),
                errorMessage: "Name is required."));

            Validator.AddRule(
                targetName: nameof(Name),
                validateDelegate: () => RuleResult.Assert(Regex.IsMatch(Name, "^\\S+$"),
                errorMessage: "Name cannot contain whitespaces."));

            //Validator.AddRule(
            //    targetName: nameof(Name),
            //    validateDelegate: () => RuleResult.Assert(Name == clip.Name || clipService.IsUniqueName(Name),
            //    errorMessage: $"Name {Name} is already used. Please choose another one."));
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Bitmap => clip?.Bitmap;

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
                    && clip?.Name != value)
                {
                    clip.Name = value;

                    eventAggregator
                        .GetEvent<ClipUpdatedEvent>()
                        .Publish(clip);
                }
            }
        }

        public DelegateCommand OnClickCommand { get; }

        public Template Template
        {
            get { return clip?.Template; }
            set
            {
                clip.Template = value;
                RaisePropertyChanged(nameof(Template));
            }
        }

        public ObservableCollection<Template> Templates { get; } = new ObservableCollection<Template>();

        public int ThresholdMonochrome
        {
            get { return clip?.ThresholdMonochrome ?? 0; }
            set
            {
                if (value >= 0
                    && value <= 100
                    && clip?.ThresholdMonochrome != value)
                {
                    clip.ThresholdMonochrome = value;

                    eventAggregator
                        .GetEvent<ClipUpdatedEvent>()
                        .Publish(clip);
                }

                RaisePropertyChanged(nameof(ThresholdMonochrome));
            }
        }

        public string Value => !string.IsNullOrWhiteSpace(clip?.Value)
            ? $"=> {clip.Value}"
            : default;

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Clip clip, IClipService clipService)
        {
            this.clip = clip;
            this.clipService = clipService;

            Name = clip?.Name;

            UpdateTemplates();
        }

        #endregion Public Methods

        #region Private Methods

        private void OnTemplateSelected()
        {
            Template = clip.Template;

            RaisePropertyChanged(nameof(Template));
            RaisePropertyChanged(nameof(Templates));
        }

        private void UpdateBitmap()
        {
            RaisePropertyChanged(nameof(Bitmap));
            RaisePropertyChanged(nameof(Value));
        }

        private void UpdateTemplates()
        {
            var givens = clipService?.TemplateService?.Templates;

            var toBeRemoveds = Templates
                .Where(t => givens?.Contains(t) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Templates.Remove(toBeRemoved);
            }

            var toBeAddeds = givens
                .Where(t => !Templates.Contains(t)).ToArray();

            Templates.AddRange(toBeAddeds);

            OnTemplateSelected();
        }

        #endregion Private Methods
    }
}