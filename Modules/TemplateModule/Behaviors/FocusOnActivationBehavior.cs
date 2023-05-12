using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Score2Stream.TemplateModule.Behaviors
{
    public class FocusOnActivationBehavior
        : Behavior<TextBox>
    {
        #region Public Fields

        public static readonly StyledProperty<string> IsActiveProperty = AvaloniaProperty.Register<FocusOnActivationBehavior, string>(
            name: nameof(IsActive));

        #endregion Public Fields

        #region Public Properties

        public string IsActive
        {
            get => GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        #endregion Public Properties
    }
}