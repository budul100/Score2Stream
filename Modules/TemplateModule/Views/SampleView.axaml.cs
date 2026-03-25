using Avalonia;
using Avalonia.Controls;

namespace Score2Stream.TemplateModule.Views;

public partial class SampleView
    : UserControl
{
    #region Public Fields

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<SampleView, bool>(
        name: nameof(IsSelected));

    #endregion Public Fields

    #region Public Constructors

    public SampleView()
    {
        InitializeComponent();

        if (ValueTextBox != default)
        {
            ValueTextBox.AttachedToVisualTree += (s, e) => OnAttachedToVisualTree();
        }
    }

    #endregion Public Constructors

    #region Public Properties

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    #endregion Public Properties

    #region Protected Methods

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == IsSelectedProperty
            && IsSelected)
        {
            SetFocusOnTextBox();
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void OnAttachedToVisualTree()
    {
        SetFocusOnTextBox();
    }

    private void SetFocusOnTextBox()
    {
        if (IsSelected
            && ValueTextBox != default)
        {
            ValueTextBox.Focus();
        }
    }

    #endregion Private Methods
}