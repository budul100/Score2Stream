using Avalonia.Media.Imaging;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Core.Events.Sample;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Events.Video;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models;
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

        public Bitmap Bitmap => Template?.Clip?.Bitmap;

        public string Current => GetCurrent();

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

        #region Public Methods

        public void SelectNext(bool onward)
        {
            if (inputService != default
                && Samples.Count > 0)
            {
                var current = Samples
                    .SingleOrDefault(s => s.Sample == inputService?.SampleService.Sample);

                var index = Samples.IndexOf(current);
                var next = default(Sample);

                if (onward)
                {
                    next = index < Samples.Count - 1
                        ? Samples[index + 1].Sample
                        : Samples[0].Sample;
                }
                else
                {
                    next = index > 0
                        ? Samples[index - 1].Sample
                        : Samples[^1].Sample;
                }

                inputService.SampleService.Select(next);
            }
        }

        #endregion Public Methods

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

            if (Template?.Samples?.Any() == true)
            {
                var toBeAddeds = Template.Samples
                    .Where(t => !Samples.Any(s => s.Sample == t)).ToArray();

                foreach (var toBeAdded in toBeAddeds)
                {
                    var current = containerProvider.Resolve<SampleViewModel>();

                    current.Initialize(
                        sample: toBeAdded,
                        sampleService: inputService.SampleService,
                        parent: this);

                    Samples.Add(current);
                }
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