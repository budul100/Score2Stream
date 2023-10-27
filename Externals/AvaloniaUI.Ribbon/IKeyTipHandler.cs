using Avalonia.Input;

namespace AvaloniaUI.Ribbon
{
    public interface IKeyTipHandler
    {
        #region Public Methods

        void ActivateKeyTips(Ribbon ribbon, IKeyTipHandler prev);

        bool HandleKeyTipKeyPress(Key key);

        #endregion Public Methods
    }
}