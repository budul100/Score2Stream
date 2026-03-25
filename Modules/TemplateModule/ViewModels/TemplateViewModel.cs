using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Score2Stream.Commons.Events.Menu;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Segment;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class TemplateViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IDialogService dialogService;
        private readonly IInputService inputService;
        private readonly Func<SampleViewModel> sampleViewModelFactory;
        private readonly ITemplateService templateService;

        private bool isDetection;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(ITemplateService templateService, IInputService inputService,
            Func<SampleViewModel> sampleViewModelFactory, IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            this.inputService = inputService;
            this.templateService = templateService;
            this.sampleViewModelFactory = sampleViewModelFactory;
            this.dialogService = dialogService;

            eventAggregator.GetEvent<DetectionChangedEvent>().Subscribe(
                action: () => IsDetection = templateService.SampleService.IsDetection,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => OnSegmentSelected(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: RefreshSamples,
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesOrderedEvent>().Subscribe(
                action: OrderSamples,
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SegmentSelectedEvent>().Subscribe(
                action: _ => OnSegmentSelected(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SegmentUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Description)),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == inputService.AreaService?.ActiveSegment);

            eventAggregator.GetEvent<SegmentDrawnEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Bitmap)),
                threadOption: ThreadOption.UIThread,
                keepSubscriberReferenceAlive: true,
                filter: s => s == inputService.AreaService?.ActiveSegment);
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap => inputService.AreaService?.ActiveSegment?.Bitmap;

        public DelegateCommand CloseCommand { get; private set; }

        public string Description => inputService.AreaService?.ActiveSegment?.GetDescription(
            showEmptyValue: false,
            includeType: true);

        public string Empty
        {
            get => Template?.Empty;
            set
            {
                if (Template == null || Template.Empty == value) return;

                Template.Empty = value;
                RaisePropertyChanged(nameof(Empty));
            }
        }

        public bool IsDetection
        {
            get => isDetection;
            set => SetProperty(ref isDetection, value);
        }

        public string Name => Template?.Name;

        public ObservableCollection<SampleViewModel> Samples { get; private set; } = [];

        public Template Template { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Template template)
        {
            Template = template;

            CloseCommand = new DelegateCommand(async () => await RemoveAsync());

            RaisePropertyChanged(nameof(Name));

            RefreshSamples();
            OnSegmentSelected();
        }

        #endregion Public Methods

        #region Private Methods

        private void OnSegmentSelected()
        {
            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Bitmap));
        }

        private void OrderSamples()
        {
            var ordered = Samples
                .OrderBy(s => s.Sample.Index).ToArray();

            for (var newIndex = 0; newIndex < ordered.Length; newIndex++)
            {
                var oldIndex = Samples.IndexOf(ordered[newIndex]);

                if (oldIndex != newIndex)
                {
                    Samples.Move(
                        oldIndex: oldIndex,
                        newIndex: newIndex);
                }
            }
        }

        private void RefreshSamples()
        {
            var currents = templateService?.SampleService?.Samples?.ToArray();

            var toBeRemoveds = Samples
                .Where(v => currents?.Contains(v.Sample) != true).ToArray();

            foreach (var toBeRemoved in toBeRemoveds)
            {
                Samples.Remove(toBeRemoved);
            }

            if (currents?.Length > 0)
            {
                var toBeAddeds = currents
                    .Where(s => s.Image != default
                        && !Samples.Any(v => v.Sample == s)).ToArray();

                foreach (var toBeAdded in toBeAddeds)
                {
                    var sample = sampleViewModelFactory.Invoke();

                    sample.Initialize(
                        sample: toBeAdded,
                        templateService: templateService);

                    Samples.Add(sample);
                }
            }
        }

        private async Task RemoveAsync()
        {
            if (Template == default) return;

            var canBeRemoved = !((Samples?.Count > 0) || !string.IsNullOrWhiteSpace(Empty));

            if (!canBeRemoved)
            {
                var messageBoxResult = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: $"Shall {Name} be removed?",
                    contentTitle: "Remove template");

                canBeRemoved = messageBoxResult == ButtonResult.Yes;
            }

            if (canBeRemoved)
            {
                templateService.Remove(Template);
            }
        }

        #endregion Private Methods
    }
}