using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Score2Stream.TemplateModule.Behaviors
{
    public class FocusOnSelectionBehavior
        : Behavior<TextBox>
    {
        #region Public Fields

        public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<FocusOnSelectionBehavior, bool>(
            name: nameof(IsSelected));

        #endregion Public Fields

        #region Public Properties

        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        #endregion Public Properties
    }
}