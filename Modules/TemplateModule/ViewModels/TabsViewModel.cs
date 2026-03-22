using System;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
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
        private TabViewModel selectedTab;

        #endregion Private Fields

        #region Public Constructors

        public TabsViewModel(ITemplateService templateService, Func<TemplateViewModel> templateViewModelFactory,
            IEventAggregator eventAggregator, IRegionManager regionManager)
            : base(regionManager)
        {
            this.templateService = templateService;
            this.templateViewModelFactory = templateViewModelFactory;

            eventAggregator.GetEvent<TemplatesChangedEvent>().Subscribe(
                action: RefreshTabs,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<TemplateSelectedEvent>().Subscribe(
                action: _ => RefreshTabs(),
                keepSubscriberReferenceAlive: true);

            RefreshTabs();
        }

        #endregion Public Constructors

        #region Public Properties

        public TabViewModel Tab
        {
            get => selectedTab;
            set
            {
                if (isRefreshing) return;

                SetProperty(ref selectedTab, value);

                if (value != default
                    && templateService.Active != value.Template)
                {
                    templateService.Select(value.Template);
                }
            }
        }

        public ObservableCollection<TabViewModel> Tabs { get; } = [];

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

            var selected = default(TabViewModel);

            foreach (var relevant in relevants)
            {
                var viewModel = templateViewModelFactory.Invoke();
                var closeCommand = new DelegateCommand(async () =>
                    await templateService.RemoveAsync(relevant));

                var tab = new TabViewModel(
                    Template: relevant,
                    Name: relevant.Name,
                    Content: viewModel,
                    CloseCommand: closeCommand);

                Tabs.Add(tab);

                if (relevant == templateService.Active)
                {
                    selected = tab;
                }
            }

            isRefreshing = false;

            Tab = selected;

            RaisePropertyChanged(nameof(Tabs));
        }

        #endregion Private Methods
    }
}