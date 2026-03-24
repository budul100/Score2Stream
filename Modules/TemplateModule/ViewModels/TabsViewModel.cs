using System;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Events;
using Prism.Regions;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;

namespace Score2Stream.TemplateModule.ViewModels
{
    public class TabsViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly ITemplateService templateService;
        private readonly Func<TemplateViewModel> templateViewModelFactory;

        private bool isRefreshing;
        private TemplateViewModel tab;

        #endregion Private Fields

        #region Public Constructors

        public TabsViewModel(ITemplateService templateService, Func<TemplateViewModel> templateViewModelFactory,
            IEventAggregator eventAggregator, IRegionManager regionManager)
            : base(regionManager)
        {
            this.templateService = templateService;
            this.templateViewModelFactory = templateViewModelFactory;

            // The selected event is needed to show the loading status of the tab.

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => RefreshTabs(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: RefreshTabs,
                keepSubscriberReferenceAlive: true);

            RefreshTabs();
        }

        #endregion Public Constructors

        #region Public Properties

        public TemplateViewModel Tab
        {
            get => tab;
            set
            {
                if (isRefreshing) return;

                SetProperty(ref tab, value);

                if (value != default
                    && templateService.Active != value.Template)
                {
                    templateService.Select(value.Template);
                }
            }
        }

        public ObservableCollection<TemplateViewModel> Tabs { get; } = [];

        #endregion Public Properties

        #region Public Methods

        public override void OnNavigatedTo(NavigationContext navigationContext)
        { }

        #endregion Public Methods

        #region Private Methods

        private void RefreshTabs()
        {
            if (isRefreshing) return;

            isRefreshing = true;

            Tabs.Clear();

            var relevants = templateService.Templates
                .Where(i => i.SampleService != default).ToArray();

            var selected = default(TemplateViewModel);

            foreach (var relevant in relevants)
            {
                var viewModel = templateViewModelFactory.Invoke();

                viewModel.Initialize(relevant);

                Tabs.Add(viewModel);

                if (relevant == templateService.Active)
                {
                    selected = viewModel;
                }
            }

            isRefreshing = false;

            Tab = selected;

            RaisePropertyChanged(nameof(Tabs));
        }

        #endregion Private Methods
    }
}