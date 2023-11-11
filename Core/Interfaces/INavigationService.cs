using Score2Stream.Core.Enums;

namespace Score2Stream.Core.Interfaces
{
    public interface INavigationService
    {
        #region Public Properties

        ViewType? EditView { get; }

        #endregion Public Properties
    }
}