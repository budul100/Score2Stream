using AvaloniaUI.Ribbon.Enums;

namespace AvaloniaUI.Ribbon.Interfaces
{
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
}