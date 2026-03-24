using System;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Events;
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
        private InputViewModel tab;

        #endregion Private Fields

        #region Public Constructors

        public TabsViewModel(IInputService inputService, Func<InputViewModel> inputViewModelFactory,
            IEventAggregator eventAggregator, IRegionManager regionManager)
            : base(regionManager)
        {
            this.inputService = inputService;
            this.inputViewModelFactory = inputViewModelFactory;

            // The selected event is needed to show the loading status of the tab.

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

        public InputViewModel Tab
        {
            get => tab;
            set
            {
                if (isRefreshing) return;

                SetProperty(ref tab, value);

                if (value != default
                    && inputService.Active != value.Input)
                {
                    inputService.Select(value.Input);
                }
            }
        }

        public ObservableCollection<InputViewModel> Tabs { get; } = [];

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

            var selected = default(InputViewModel);

            foreach (var relevant in relevants)
            {
                var viewModel = inputViewModelFactory.Invoke();

                viewModel.Initialize(relevant);

                Tabs.Add(viewModel);

                if (relevant == inputService.Active)
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