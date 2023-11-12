using Avalonia.Controls.Templates;

namespace AvaloniaUI.Ribbon.Interfaces
{
    public interface ICanAddToQuickAccess
    {
        #region Public Properties

        bool CanAddToQuickAccess
        {
            get;
            set;
        }

        IControlTemplate QuickAccessTemplate
        {
            get;
            set;
        }

        #endregion Public Properties
    }
}