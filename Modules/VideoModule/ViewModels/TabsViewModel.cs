using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Xaml.Interactions.Custom;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using Score2Stream.Commons.Events.Input;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Prism;

namespace Score2Stream.VideoModule.ViewModels
{
    public class TabsViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IInputService inputService;
        private readonly Func<InputViewModel> inputViewModelFactory;

        private bool isRefreshing;
        private TabViewModel selectedTab;

        #endregion Private Fields

        #region Public Constructors

        public TabsViewModel(IInputService inputService, Func<InputViewModel> inputViewModelFactory,
            IEventAggregator eventAggregator, IRegionManager regionManager)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.inputViewModelFactory = inputViewModelFactory;

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
                var viewModel = inputViewModelFactory.Invoke();
                var closeCommand = new DelegateCommand(async () =>
                    await inputService.RemoveAsync(relevant));

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
        }

        #endregion Private Methods
    }
}