using Core.Events;
using Core.Models;
using Core.Mvvm;
using Prism.Commands;
using Prism.Events;

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

            var selectEvent = eventAggregator.GetEvent<SelectTemplateEvent>();
            OnClickCommand = new DelegateCommand(() => selectEvent.Publish(clip));
        }

        #endregion Public Constructors

        #region Public Properties

        public string Name => clip.Name;

        public DelegateCommand OnClickCommand { get; }

        #endregion Public Properties
    }
}