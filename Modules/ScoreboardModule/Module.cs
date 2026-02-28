using Prism.Ioc;
using Prism.Modularity;
using Score2Stream.Commons.Enums;
using Score2Stream.ScoreboardModule.Views;

namespace Score2Stream.ScoreboardModule
{
    public class Module
        : IModule
    {
        #region Public Methods

        public void OnInitialized(IContainerProvider containerProvider)
        { }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ContentView>(
                name: nameof(ViewType.Board));
        }

        #endregion Public Methods
    }
}