using Prism.Regions;
using Score2Stream.Commons.Enums;
using Score2Stream.Commons.Interfaces;

namespace Score2Stream.NavigationService
{
    public class Service(IRegionManager regionManager)
        : INavigationService
    {
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
                    nameof(ViewType.Inputs) => (ViewType?)ViewType.Inputs,

                    nameof(ViewType.Templates) => (ViewType?)ViewType.Templates,

                    nameof(ViewType.Board) => (ViewType?)ViewType.Board,

                    _ => (ViewType?)ViewType.Inputs,
                };
            }
        }

        #endregion Public Properties
    }
}