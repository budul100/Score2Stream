using Avalonia.Controls;

namespace AvaloniaUI.Ribbon
{
    public enum RibbonControlSize
    {
        Small,
        Medium,
        Large
    }

    public interface IRibbonControl
    {
        #region Public Properties

        RibbonControlSize MaxSize
        {
            get;
            set;
        }

        RibbonControlSize MinSize
        {
            get;
            set;
        }

        RibbonControlSize Size
        {
            get;
            set;
        }

        #endregion Public Properties
    }

    public static class RibbonControlExtensions
    {
        #region Public Methods

        public static Ribbon GetParentRibbon(this Control control)
        {
            return Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<Ribbon>(control, true);
            /*IControl parentRbn = control.Parent;
            while ((!(parentRbn is Ribbon)) && (parentRbn != null))
            {
                parentRbn = parentRbn.Parent;
                /*if (parentRbn == null)
                    break;*
            }

            if (parentRbn is Ribbon ribbon)
                return ribbon;
            else
                return null;*/
        }

        #endregion Public Methods
    }
}