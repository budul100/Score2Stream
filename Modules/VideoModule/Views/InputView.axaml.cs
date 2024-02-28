using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Score2Stream.VideoModule.ViewModels;

namespace Score2Stream.VideoModule.Views;

public partial class InputView
    : UserControl
{
    #region Public Constructors

    public InputView()
    {
        InitializeComponent();

        var dataContext = this.DataContext as InputViewModel;
        var zoomBorder = this.FindControl<ZoomBorder>("InputBorder");

        if (dataContext != default
            && zoomBorder != default)
        {
            dataContext.OnVideoCentredEvent += (s, e) => zoomBorder.ResetMatrix();
        }
    }

    #endregion Public Constructors
}