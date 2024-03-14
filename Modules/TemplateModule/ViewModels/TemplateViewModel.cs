using Avalonia.Media.Imaging;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Detection;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Prism;
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

            eventAggregator.GetEvent<ClipDrawnEvent>().Subscribe(
                action: _ => UpdateImage(),
                keepSubscriberReferenceAlive: true);

            UpdateTemplate(inputService?.TemplateService?.Template);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => inputService.AreaService?.Segment?.Bitmap;

        public string Description => inputService.AreaService?.Segment?.GetDescription(true);

        public string Empty
        {
            get { return Template?.Empty; }
            set
            {
                Template.Empty = value;

                RaisePropertyChanged(nameof(Empty));
            }
        }

        public bool IsDetection
        {
            get { return isDetection; }
            set { SetProperty(ref isDetection, value); }
        }

        public ObservableCollection<SampleViewModel> Samples { get; private set; } = new ObservableCollection<SampleViewModel>();

        public Template Template { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private void OrderSamples()
        {
            Samples = new ObservableCollection<SampleViewModel>(Samples.OrderBy(s => s.Sample.Index));

            RaisePropertyChanged(nameof(Samples));
        }

        private void UpdateImage()
        {
            RaisePropertyChanged(nameof(Bitmap));
            RaisePropertyChanged(nameof(Description));
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

                OrderSamples();
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