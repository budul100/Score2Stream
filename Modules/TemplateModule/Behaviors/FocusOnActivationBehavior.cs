using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Score2Stream.TemplateModule.Behaviors
{
    public class FocusOnActivationBehavior
        : Behavior<TextBox>
    {
        #region Public Fields

        public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<FocusOnActivationBehavior, bool>(
            name: nameof(IsActive));

        #endregion Public Fields

        #region Public Properties

        public bool IsActive
        {
            get => GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        #endregion Public Properties
    }
}