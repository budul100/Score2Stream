using Core.Events.Samples;
using Core.Interfaces;
using Core.Models;
using Core.Prism;
using Prism.Commands;
using Prism.Events;
using System.Windows.Media.Imaging;

namespace TemplateModule.ViewModels
{
    public class SampleViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private bool isActive;
        private Sample sample;
        private ISampleService sampleService;

        #endregion Private Fields

        #region Public Constructors

        public SampleViewModel(IEventAggregator eventAggregator)
        {
            OnClickCommand = new DelegateCommand(
                executeMethod: () => sampleService.Select(sample));

            eventAggregator.GetEvent<SampleSelectedEvent>().Subscribe(
                action: s => IsActive = s == sample,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Bitmap => sample?.Bitmap;

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public DelegateCommand OnClickCommand { get; }

        public string Value
        {
            get { return sample?.Value; }
            set
            {
                sample.Value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Sample sample, ISampleService sampleService)
        {
            this.sample = sample;
            this.sampleService = sampleService;

            Value = sample.Value;

            RaisePropertyChanged(nameof(Bitmap));
        }

        #endregion Public Methods
    }
}