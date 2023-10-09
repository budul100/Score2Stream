using Avalonia;
using Avalonia.Controls;

namespace Score2Stream.TemplateModule.Views;

public partial class SampleView
    : UserControl
{
    #region Public Fields

    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<SampleView, bool>(
        name: nameof(IsActive));

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

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    #endregion Public Properties

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

    private void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (IsActive
            && e.Property.Name == nameof(IsActive))
        {
            SetFocusOnTextBox();
        }
    }

    private void SetFocusOnTextBox()
    {
        if (IsActive
            && ValueTextBox != default)
        {
            ValueTextBox.Focus();
        }
    }

    #endregion Private Methods
}