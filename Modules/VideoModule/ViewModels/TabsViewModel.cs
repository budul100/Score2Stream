using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;

namespace Score2Stream.VideoModule.ViewModels
{
    public class TabsViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IInputService inputService;

        private bool isRefreshing;
        private TabViewModel selectedTab;

        #endregion Private Fields

        #region Public Constructors

        public TabsViewModel(IInputService inputService, IContainerProvider containerProvider,
            IEventAggregator eventAggregator, IRegionManager regionManager)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.containerProvider = containerProvider;

            eventAggregator.GetEvent<InputSelectedEvent>().Subscribe(
                action: _ => RefreshTabs(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<InputStartedEvent>().Subscribe(
                action: _ => RefreshTabs(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<InputEndedEvent>().Subscribe(
                action: _ => RefreshTabs(),
                keepSubscriberReferenceAlive: true);

            RefreshTabs();
        }

        #endregion Public Constructors

        #region Public Properties

        public bool ShowTabHeaders => Tabs.Count > 1;

        public TabViewModel Tab
        {
            get => selectedTab;
            set
            {
                if (isRefreshing) return;

                SetProperty(ref selectedTab, value);

                if (value != default
                    && inputService.Active != value.Input)
                {
                    inputService.Select(value.Input);
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

            var relevants = inputService.Inputs
                .Where(i => i.VideoService != default
                    && i.IsStarted).ToArray();

            var selected = default(TabViewModel);

            foreach (var relevant in relevants)
            {
                var viewModel = containerProvider.Resolve<InputViewModel>();
                var closeCommand = new DelegateCommand(async () => await inputService.StopAsync(relevant));

                var tab = new TabViewModel(
                    Input: relevant,
                    Name: relevant.Name,
                    Content: viewModel,
                    CloseCommand: closeCommand);

                Tabs.Add(tab);

                if (relevant == inputService.Active)
                {
                    selected = tab;
                }
            }

            isRefreshing = false;

            Tab = selected;

            RaisePropertyChanged(nameof(Tabs));
            RaisePropertyChanged(nameof(ShowTabHeaders));
        }

        #endregion Private Methods
    }
}