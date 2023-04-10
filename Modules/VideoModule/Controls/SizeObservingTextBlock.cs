using System.Windows;
using System.Windows.Controls;

namespace VideoModule.Controls
{
    public class SizeObservingTextBlock
        : TextBlock
    {
        // Using a DependencyProperty as the backing store for CHeight and CWidth.
        // This enables animation, styling, binding, etc...

        #region Public Fields

        public static readonly DependencyProperty CHeightProperty = DependencyProperty.Register(
            name: "CHeight",
            propertyType: typeof(double),
            ownerType: typeof(SizeObservingTextBlock));

        public static readonly DependencyProperty CWidthProperty = DependencyProperty.Register(
            name: "CWidth",
            propertyType: typeof(double),
            ownerType: typeof(SizeObservingTextBlock));

        #endregion Public Fields

        #region Public Constructors

        public SizeObservingTextBlock()
        {
            this.SizeChanged += OnSizeChanged;
        }

        #endregion Public Constructors

        #region Public Properties

        public double CHeight
        {
            get { return (double)GetValue(CHeightProperty); }
            set { SetValue(CHeightProperty, value); }
        }

        public double CWidth
        {
            get { return (double)GetValue(CWidthProperty); }
            set { SetValue(CWidthProperty, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CHeight = e.NewSize.Height;
            CWidth = e.NewSize.Width;
        }

        #endregion Private Methods
    }
}