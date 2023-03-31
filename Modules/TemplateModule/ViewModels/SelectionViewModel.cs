using Core.Events;
using Prism.Events;
using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace TemplateModule.ViewModels
{
    public class SelectionViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        private Template template;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(ITemplateService templateService, IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            this.eventAggregator = eventAggregator;

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: t => SetCurrent(t),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => OnUpdateCurrent(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: _ => UpdateTemplate(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<WebcamUpdatedEvent>().Subscribe(
                action: () => OnUpdateCurrent(),
                keepSubscriberReferenceAlive: true);

            SetCurrent(templateService.Template);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Content => template?.Clip?.Content;

        public string Current => !string.IsNullOrWhiteSpace(template?.Clip?.Value)
            ? $"{template.Name} => {template.Clip.Value}"
            : template?.Name;

        public ObservableCollection<SampleViewModel> Samples { get; } = new ObservableCollection<SampleViewModel>();

        #endregion Public Properties

        #region Private Methods

        private void OnUpdateCurrent()
        {
            RaisePropertyChanged(nameof(Content));
            RaisePropertyChanged(nameof(Current));
        }

        private void SetCurrent(Template template)
        {
            this.template = template;

            UpdateTemplate();

            OnUpdateCurrent();
        }

        private void UpdateTemplate()
        {
            Samples.Clear();

            if (this.template != default)
            {
                foreach (var sample in this.template?.Samples)
                {
                    var current = new SampleViewModel(
                        sample: sample,
                        eventAggregator: eventAggregator);

                    Samples.Add(current);
                }
            }
        }

        #endregion Private Methods
    }
}