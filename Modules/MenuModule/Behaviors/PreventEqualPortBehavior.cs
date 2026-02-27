using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Score2Stream.MenuModule.Behaviors
{
    public class PreventEqualPortBehavior
        : Behavior<NumericUpDown>
    {
        #region Public Fields

        public static readonly StyledProperty<NumericUpDown> OtherPortProperty =
            AvaloniaProperty.Register<PreventEqualPortBehavior, NumericUpDown>(nameof(OtherPort));

        #endregion Public Fields

        #region Public Properties

        public NumericUpDown OtherPort
        {
            get => GetValue(OtherPortProperty);
            set => SetValue(OtherPortProperty, value);
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject != null)
                AssociatedObject.ValueChanged += OnValueChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
                AssociatedObject.ValueChanged -= OnValueChanged;
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
        {
            if (AssociatedObject == null || OtherPort == null) return;

            if (AssociatedObject.Value == OtherPort.Value)
            {
                var goingUp = e.NewValue > e.OldValue;

                var jumped = goingUp
                    ? e.NewValue + AssociatedObject.Increment
                    : e.NewValue - AssociatedObject.Increment;

                if (jumped >= AssociatedObject.Minimum
                    && jumped <= AssociatedObject.Maximum)
                {
                    AssociatedObject.Value = jumped;
                }
                else
                {
                    AssociatedObject.Value = e.OldValue;
                }
            }
        }

        #endregion Private Methods
    }
}