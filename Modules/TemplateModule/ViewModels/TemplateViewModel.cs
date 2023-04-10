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
        private readonly IInputService inputService;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(IInputService inputService, IContainerProvider containerProvider,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.containerProvider = containerProvider;

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: t => UpdateTemplate(t),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: () => UpdateSamples(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<OrderSamplesEvent>().Subscribe(
                action: () => OrderSamples(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => UpdateImage(),
                keepSubscriberReferenceAlive: true);

            UpdateTemplate(inputService?.TemplateService?.Template);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Bitmap => Template?.Clip?.Bitmap;

        public string Current => !string.IsNullOrWhiteSpace(Template?.Clip?.Value)
            ? $"{Template.Name} => {Template.Clip.Value}"
            : Template?.Name;

        public ObservableCollection<SampleViewModel> Samples { get; } = new ObservableCollection<SampleViewModel>();

        public Template Template { get; private set; }

        public string ValueEmpty
        {
            get { return Template?.ValueEmpty; }
            set
            {
                Template.ValueEmpty = value;

                RaisePropertyChanged(nameof(ValueEmpty));
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void OrderSamples()
        {
            var ordereds = Samples
                .OrderByDescending(s => s.HasNoValue)
                .ThenBy(s => s.Value).ToArray();

            Samples.Clear();

            foreach (var ordered in ordereds)
            {
                Samples.Add(ordered);
            }
        }

        private void UpdateImage()
        {
            RaisePropertyChanged(nameof(Bitmap));
            RaisePropertyChanged(nameof(Current));
        }

        private void UpdateSamples()
        {
            var toBeRemoveds = Samples
                .Where(s => Template.Samples?.Contains(s.Sample) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Samples.Remove(toBeRemoved);
            }

            var toBeAddeds = Template.Samples
                .Where(t => !Samples.Any(s => s.Sample == t)).ToArray();

            foreach (var toBeAdded in toBeAddeds)
            {
                var current = containerProvider.Resolve<SampleViewModel>();

                current.Initialize(
                    sample: toBeAdded,
                    sampleService: inputService.SampleService);

                Samples.Add(current);
            }
        }

        private void UpdateTemplate(Template template)
        {
            this.Template = template;

            UpdateImage();
            UpdateSamples();
        }

        #endregion Private Methods
    }
}