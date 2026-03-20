using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media.Imaging;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Events.Clip;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Prism;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class TemplateViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IInputService inputService;
        private readonly ITemplateService templateService;

        private bool isDetection;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(IInputService inputService, ITemplateService templateService,
            IContainerProvider containerProvider, IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.templateService = templateService;
            this.containerProvider = containerProvider;

            eventAggregator.GetEvent<DetectionChangedEvent>().Subscribe(
                action: () => IsDetection = templateService.SampleService.IsDetection,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnSegmentSelected(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: RefreshSamples,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesOrderedEvent>().Subscribe(
                action: OrderSamples,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SegmentSelectedEvent>().Subscribe(
                action: _ => OnSegmentSelected(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SegmentUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Description)),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == inputService.AreaService?.Segment);

            eventAggregator.GetEvent<SegmentDrawnEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Bitmap)),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == inputService.AreaService?.Segment);

            this.Template = templateService?.Active;

            RefreshSamples();
            OnSegmentSelected();
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

        public ObservableCollection<SampleViewModel> Samples { get; private set; } = [];

        public Template Template { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private void OnSegmentSelected()
        {
            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Bitmap));
            RaisePropertyChanged(nameof(IsDetection));
        }

        private void OrderSamples()
        {
            Samples = new ObservableCollection<SampleViewModel>(Samples.OrderBy(s => s.Sample.Index));

            RaisePropertyChanged(nameof(Samples));
        }

        private void RefreshSamples()
        {
            var toBeRemoveds = Samples
                .Where(s => Template.Samples?.Contains(s.Sample) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Samples.Remove(toBeRemoved);
            }

            if (Template?.Samples?.Count > 0)
            {
                var toBeAddeds = Template.Samples
                    .Where(s => s.Image != default
                        && !Samples.Any(m => m.Sample == s))
                    .OrderBy(s => s.Index).ToArray();

                foreach (var toBeAdded in toBeAddeds)
                {
                    var current = containerProvider.Resolve<SampleViewModel>();

                    current.Initialize(
                        sample: toBeAdded,
                        areaService: inputService.AreaService,
                        sampleService: templateService.SampleService);

                    Samples.Add(current);
                }
            }
        }

        #endregion Private Methods
    }
}