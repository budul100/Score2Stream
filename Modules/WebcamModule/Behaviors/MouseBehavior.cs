using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WebcamModule.Behaviors
{
    public class MouseBehavior
        : Behavior<Grid>
    {
        #region Public Fields

        public static readonly DependencyProperty MouseXProperty = DependencyProperty.Register(
            name: "MouseX",
            propertyType: typeof(double),
            ownerType: typeof(MouseBehavior),
            typeMetadata: new PropertyMetadata(default(double)));

        public static readonly DependencyProperty MouseYProperty = DependencyProperty.Register(
            name: "MouseY",
            propertyType: typeof(double),
            ownerType: typeof(MouseBehavior),
            typeMetadata: new PropertyMetadata(default(double)));

        #endregion Public Fields

        #region Public Properties

        public double MouseX
        {
            get { return (double)GetValue(MouseXProperty); }
            set { SetValue(MouseXProperty, value); }
        }

        public double MouseY
        {
            get { return (double)GetValue(MouseYProperty); }
            set { SetValue(MouseYProperty, value); }
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnAttached()
        {
            AssociatedObject.MouseMove += AssociatedObjectOnMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseMove -= AssociatedObjectOnMouseMove;
        }

        #endregion Protected Methods

        #region Private Methods

        private void AssociatedObjectOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var pos = mouseEventArgs.GetPosition(AssociatedObject);
            MouseX = pos.X;
            MouseY = pos.Y;
        }

        #endregion Private Methods
    }
}