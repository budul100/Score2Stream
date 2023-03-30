using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClipModule.Helpers
{
    public class IsActiveToColourConverter
        : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = (bool)value
                ? new SolidColorBrush(Colors.LightBlue)
                : new SolidColorBrush(Colors.Transparent);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }
}