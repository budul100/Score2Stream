using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Custom;
using Avalonia.Xaml.Interactivity;

namespace Score2Stream.VideoModule.Behaviors
{
    public class SizeChangedBehavior
        : Behavior<Control>
    {
        #region Public Fields

        public static readonly StyledProperty<object> HeightProperty = AvaloniaProperty.Register<ValueChangedTriggerBehavior, object>(
            name: nameof(Height));

        public static readonly StyledProperty<object> WidthProperty = AvaloniaProperty.Register<ValueChangedTriggerBehavior, object>(
            name: nameof(Width));

        #endregion Public Fields

        #region Public Properties

        public object Height
        {
            get => GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        public object Width
        {
            get => GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnAttachedToVisualTree()
        {
            if (AssociatedObject is { })
            {
                AssociatedObject.PropertyChanged += OnPropertyChanged;
            }
        }

        protected override void OnDetachedFromVisualTree()
        {
            if (AssociatedObject is { })
            {
                AssociatedObject.PropertyChanged -= OnPropertyChanged;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(Visual.Bounds))
            {
                var rect = (Rect)e.NewValue;

                Height = rect != default && !double.IsNaN(rect.Height)
                    ? rect.Height
                    : 0;

                Width = rect != default && !double.IsNaN(rect.Width)
                    ? rect.Width
                    : 0;
            }
        }

        #endregion Private Methods
    }
}