using Core.Events;
using Core.Models;
using Prism.Commands;
using Prism.Events;
using Core.Mvvm;
using System.Windows.Media.Imaging;

namespace TemplateModule.ViewModels
{
    public class SampleViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly Sample sample;

        private bool isActive;

        #endregion Private Fields

        #region Public Constructors

        public SampleViewModel(Sample sample, IEventAggregator eventAggregator)
        {
            this.sample = sample;

            Content = sample.Content;
            Value = sample.Value;

            var selectEvent = eventAggregator.GetEvent<SelectSampleEvent>();
            OnClickCommand = new DelegateCommand(
                executeMethod: () => selectEvent.Publish(sample));

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsActive = s == sample,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Content { get; }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public DelegateCommand OnClickCommand { get; }

        public string Value
        {
            get { return sample.Value; }
            set
            {
                sample.Value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }

        #endregion Public Properties
    }
}