using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Score2Stream.Commons.Converters
{
    public class BoolToHighlightBrushConverter
        : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type type, object parameter, CultureInfo cultureInfo)
        {
            if (value is true) return Brushes.Transparent;

            if (parameter is string colorString
                && Color.TryParse(colorString, out var color))
            {
                return new SolidColorBrush(color);
            }

            return Brushes.Orange;
        }

        public object ConvertBack(object value, Type type, object parameter, CultureInfo cultureInfo) =>
            throw new NotSupportedException();

        #endregion Public Methods
    }
}