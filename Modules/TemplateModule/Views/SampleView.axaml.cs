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
            ValueTextBox.DetachedFromVisualTree += (s, e) => OnDetachedFromVisualTree();
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
        if (IsSelected
            && e.Property.Name == nameof(IsSelected))
        {
            SetFocusOnTextBox();
        }

        base.OnPropertyChanged(e);
    }

    #endregion Protected Methods

    #region Private Methods

    private void OnAttachedToVisualTree()
    {
        this.PropertyChanged += (s, e) => OnPropertyChanged(e);

        SetFocusOnTextBox();
    }

    private void OnDetachedFromVisualTree()
    {
        this.PropertyChanged -= (s, e) => OnPropertyChanged(e);
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