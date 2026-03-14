using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Score2Stream.VideoModule.Behaviors
{
    public class PointerPressedBehavior
        : Behavior<Control>
    {
        #region Public Fields

        public static readonly StyledProperty<ICommand> CommandProperty =
            AvaloniaProperty.Register<PointerPressedBehavior, ICommand>(
                name: nameof(Command));

        public static readonly StyledProperty<bool> MarkHandledProperty =
            AvaloniaProperty.Register<PointerPressedBehavior, bool>(
                name: nameof(MarkHandled),
                defaultValue: true);

        #endregion Public Fields

        #region Public Properties

        public ICommand Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public bool MarkHandled
        {
            get => GetValue(MarkHandledProperty);
            set => SetValue(MarkHandledProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnAttachedToVisualTree()
        {
            if (AssociatedObject is { })
            {
                AssociatedObject.PointerPressed += OnPointerPressed;
            }
        }

        protected override void OnDetachedFromVisualTree()
        {
            if (AssociatedObject is { })
            {
                AssociatedObject.PointerPressed -= OnPointerPressed;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (Command?.CanExecute(default) == true)
            {
                Command.Execute(default);

                if (MarkHandled)
                {
                    e.Handled = true;
                }
            }
        }

        #endregion Private Methods
    }
}