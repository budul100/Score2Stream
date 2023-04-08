using Core.Events.Samples;
using Core.Events.Templates;
using Core.Events.Video;
using Core.Interfaces;
using Core.Models;
using Core.Prism;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace TemplateModule.ViewModels
{
    public class TemplateViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IInputService inputService;
        private Template template;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(IInputService inputService, IContainerProvider containerProvider,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: t => UpdateTemplate(t),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: () => UpdateSamples(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => UpdateImage(),
                keepSubscriberReferenceAlive: true);

            UpdateTemplate(inputService?.TemplateService?.Template);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Bitmap => template?.Clip?.Bitmap;

        public string Current => !string.IsNullOrWhiteSpace(template?.Clip?.Value)
            ? $"{template.Name} => {template.Clip.Value}"
            : template?.Name;

        public ObservableCollection<SampleViewModel> Samples { get; } = new ObservableCollection<SampleViewModel>();

        #endregion Public Properties

        #region Private Methods

        private void UpdateImage()
        {
            RaisePropertyChanged(nameof(Bitmap));
            RaisePropertyChanged(nameof(Current));
        }

        private void UpdateSamples()
        {
            Samples.Clear();

            if (template?.Samples?.Any() == true)
            {
                foreach (var sample in template.Samples)
                {
                    var current = containerProvider.Resolve<SampleViewModel>();

                    current.Initialize(
                        sample: sample,
                        sampleService: inputService.SampleService);

                    Samples.Add(current);
                }
            }
        }

        private void UpdateTemplate(Template template)
        {
            this.template = template;

            UpdateImage();
            UpdateSamples();
        }

        #endregion Private Methods
    }
}