using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Score2Stream.VideoModule.ViewModels;

namespace Score2Stream.VideoModule.Views;

public partial class VideoView : UserControl
{
    #region Public Constructors

    public VideoView()
    {
        InitializeComponent();

        var zoomBorder = this.FindControl<ZoomBorder>("VideoBorder");
        var dataContext = this.DataContext as VideoViewModel;

        if (dataContext != default
            && zoomBorder != default)
        {
            dataContext.OnVideoCentredEvent += (s, e) => zoomBorder.ResetMatrix();
        }
    }

    #endregion Public Constructors
}