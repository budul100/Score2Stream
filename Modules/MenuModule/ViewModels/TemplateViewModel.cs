using Core.Events;
using Prism.Commands;
using Prism.Events;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;

namespace MenuModule.ViewModels
{
    public class TemplateViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly Clip clip;
        private readonly IEventAggregator eventAggregator;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(Clip clip, IEventAggregator eventAggregator)
        {
            this.clip = clip;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Name)),
                keepSubscriberReferenceAlive: true);

            OnClickCommand = new DelegateCommand(OnClick);
        }

        #endregion Public Constructors

        #region Public Properties

        public string Name => clip.Name;

        public DelegateCommand OnClickCommand { get; }

        #endregion Public Properties

        #region Private Methods

        private void OnClick()
        {
            eventAggregator.GetEvent<SelectTemplateEvent>()
                .Publish(clip);
        }

        #endregion Private Methods
    }
}