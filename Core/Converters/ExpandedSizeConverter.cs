using System;
using System.Globalization;
using System.Windows.Data;

namespace Core.Converters
{
    public class ExpandedSizeConverter
        : IMultiValueConverter
    {
        #region Public Methods

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3)
            {
                return null;
            }

            var position = (double?)values[0];
            var givenSize = (double?)values[1];
            var actualSize = (double?)values[2];

            var result = actualSize > givenSize
                ? position - ((actualSize - givenSize) / 2)
                : position;

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(GetType().Name + " can only convert one-way.");
        }

        #endregion Public Methods
    }
}