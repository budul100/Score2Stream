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

        this.PropertyChanged += (s, e) => OnPropertyChanged(e);

        if (ValueTextBox != default)
        {
            ValueTextBox.AttachedToVisualTree += (s, e) => SetFocusOnTextBox();
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

    private void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(IsActive)
            && IsActive)
        {
            SetFocusOnTextBox();
        }
    }

    private void SetFocusOnTextBox()
    {
        if (ValueTextBox != default
            && IsActive)
        {
            ValueTextBox.Focus();
        }
    }

    #endregion Private Methods
}