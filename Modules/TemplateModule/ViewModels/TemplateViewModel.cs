using Avalonia.Media.Imaging;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Core.Events.Detection;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using Score2Stream.Core.Prism;
using System.Collections.ObjectModel;
using System.Linq;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class TemplateViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IInputService inputService;

        private bool isDetection;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(IInputService inputService, IContainerProvider containerProvider,
            IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.containerProvider = containerProvider;

            eventAggregator.GetEvent<DetectionChangedEvent>().Subscribe(
                action: () => IsDetection = inputService.SampleService.IsDetection,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: t => UpdateTemplate(t),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: () => UpdateSamples(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesOrderedEvent>().Subscribe(
                action: () => OrderSamples(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<VideoUpdatedEvent>().Subscribe(
                action: () => UpdateImage(),
                keepSubscriberReferenceAlive: true);

            UpdateTemplate(inputService?.TemplateService?.Template);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => Template?.Clip?.Bitmap;

        public string Current => GetCurrent();

        public bool IsDetection
        {
            get { return isDetection; }
            set { SetProperty(ref isDetection, value); }
        }

        public ObservableCollection<SampleViewModel> Samples { get; private set; } = new ObservableCollection<SampleViewModel>();

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

        private string GetCurrent()
        {
            var result = !string.IsNullOrWhiteSpace(Template?.Clip?.Value)
                ? $"{Template.Description} => {Template.Clip.Value} ({Template.Clip.Similarity})"
                : Template?.Description;

            return result;
        }

        private void OrderSamples()
        {
            Samples = new ObservableCollection<SampleViewModel>(Samples.OrderBy(s => s.Sample.Index));

            RaisePropertyChanged(nameof(Samples));
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

            if (Template?.Samples?.Any() == true)
            {
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

            RaisePropertyChanged(nameof(Samples));
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