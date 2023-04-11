using System;
using System.Globalization;

namespace Core.Converters
{
    public class ExpandedSizeConverter
        : OneWayMultiValueConverter
    {
        #region Public Methods

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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

        #endregion Public Methods
    }
}