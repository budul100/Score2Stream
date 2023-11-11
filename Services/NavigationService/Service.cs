using Prism.Regions;
using Score2Stream.Core.Enums;
using Score2Stream.Core.Interfaces;

namespace NavigationService
{
    public class Service
        : INavigationService
    {
        #region Private Fields

        private readonly IRegionManager regionManager;

        #endregion Private Fields

        #region Public Constructors

        public Service(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        #endregion Public Constructors

        #region Public Properties

        public ViewType? EditView
        {
            get
            {
                var viewName = regionManager.Regions[nameof(RegionType.EditRegion)]?
                    .NavigationService.Journal.CurrentEntry?
                    .Uri.OriginalString;

                return viewName switch
                {
                    nameof(ViewType.Board) => (ViewType?)ViewType.Board,

                    nameof(ViewType.Clips) => (ViewType?)ViewType.Clips,

                    nameof(ViewType.Templates) => (ViewType?)ViewType.Templates,

                    _ => default,
                };
            }
        }

        #endregion Public Properties
    }
}