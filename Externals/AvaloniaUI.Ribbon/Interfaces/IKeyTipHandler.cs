using Avalonia.Input;

namespace AvaloniaUI.Ribbon.Interfaces
{
    public interface IKeyTipHandler
    {
        #region Public Methods

        void ActivateKeyTips(Ribbon ribbon, IKeyTipHandler prev);

        bool HandleKeyTipKeyPress(Key key);

        #endregion Public Methods
    }
}