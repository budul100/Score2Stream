using Core.Events;
using Prism.Events;
using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;
using System.Windows.Media.Imaging;

namespace TemplateModule.ViewModels
{
    public class SelectionViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private Template current;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(ITemplateService templateService, IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(regionManager)
        {
            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: t => SetCurrent(t),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ClipUpdatedEvent>().Subscribe(
                action: _ => RaisePropertyChanged(nameof(Name)),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<ContentUpdatedEvent>().Subscribe(
                action: () => RaisePropertyChanged(nameof(Content)),
                keepSubscriberReferenceAlive: true);

            SetCurrent(templateService.Selection);
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Content => current?.Clip?.Content;

        public string Name => current?.Clip?.Name;

        #endregion Public Properties

        #region Private Methods

        private void SetCurrent(Template template)
        {
            current = template;

            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(Content));
        }

        #endregion Private Methods
    }
}