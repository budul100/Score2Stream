using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactions.Custom;
using Avalonia.Xaml.Interactivity;

namespace Score2Stream.VideoModule.Behaviors
{
    public class PointerPositionBehavior
        : Behavior<Control>
    {
        #region Public Fields

        public static readonly StyledProperty<object> PointerXProperty = AvaloniaProperty.Register<ValueChangedTriggerBehavior, object>(
            name: nameof(PointerX));

        public static readonly StyledProperty<object> PointerYProperty = AvaloniaProperty.Register<ValueChangedTriggerBehavior, object>(
            name: nameof(PointerY));

        #endregion Public Fields

        #region Public Properties

        public object PointerX
        {
            get => GetValue(PointerXProperty);
            set => SetValue(PointerXProperty, value);
        }

        public object PointerY
        {
            get => GetValue(PointerYProperty);
            set => SetValue(PointerYProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnAttachedToVisualTree()
        {
            if (AssociatedObject is { })
            {
                AssociatedObject.PointerMoved += OnPointerMoved;
            }
        }

        protected override void OnDetachedFromVisualTree()
        {
            if (AssociatedObject is { })
            {
                AssociatedObject.PointerMoved -= OnPointerMoved;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            if (PointerX is { })
            {
                PointerX = e.GetPosition(AssociatedObject).X;
            }

            if (PointerY is { })
            {
                PointerY = e.GetPosition(AssociatedObject).Y;
            }
        }

        #endregion Private Methods
    }
}