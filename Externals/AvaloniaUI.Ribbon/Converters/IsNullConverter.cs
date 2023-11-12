using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AvaloniaUI.Ribbon.Converters
{
    public class IsNullConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion Public Methods
    }
}